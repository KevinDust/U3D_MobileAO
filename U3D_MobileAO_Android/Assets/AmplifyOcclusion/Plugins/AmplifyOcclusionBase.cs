// Amplify Occlusion 2 - Robust Ambient Occlusion for Unity
// Copyright (c) Amplify Creations, Lda <info@amplify.pt>

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Profiling;
using AmplifyOcclusion;

#if UNITY_EDITOR
using UnityEditor;
#endif

[AddComponentMenu( "" )]
public class AmplifyOcclusionBase : MonoBehaviour
{
	private static int m_nextID = 0;
	private int m_myID;
	private string m_myIDstring;

	public enum ApplicationMethod
	{
		PostEffect = 0,
		Deferred,
		Debug
	}

	public enum PerPixelNormalSource
	{
		None = 0,
		Camera,
		GBuffer,
		GBufferOctaEncoded,
	}

	[Header( "Ambient Occlusion" )]
	[Tooltip( "How to inject the occlusion: Post Effect = Overlay, Deferred = Deferred Injection, Debug - Vizualize." )]
	public ApplicationMethod ApplyMethod = ApplicationMethod.PostEffect;

	[Tooltip( "Number of samples per pass." )]
	public SampleCountLevel SampleCount = SampleCountLevel.Medium;

	[Tooltip( "Source of per-pixel normals: None = All, Camera = Forward, GBuffer = Deferred." )]
	public PerPixelNormalSource PerPixelNormals = PerPixelNormalSource.Camera;

	[Tooltip( "Final applied intensity of the occlusion effect." )]
	[Range( 0, 1 )]
	public float Intensity = 1.0f;

	[Tooltip( "Color tint for occlusion." )]
	public Color Tint = Color.black;

	[Tooltip( "Radius spread of the occlusion." )]
	[Range( 0, 32 )]
	public float Radius = 2.0f;

	[Tooltip( "Power exponent attenuation of the occlusion." )]
	[Range( 0, 16 )]
	public float PowerExponent = 1.8f;

	[Tooltip( "Controls the initial occlusion contribution offset." )]
	[Range( 0, 0.99f )]
	public float Bias = 0.05f;

	[Tooltip( "Controls the thickness occlusion contribution." )]
	[Range( 0, 1.0f )]
	public float Thickness = 1.0f;

	[Tooltip( "Compute the Occlusion and Blur at half of the resolution." )]
	public bool Downsample = true;

	[Tooltip( "Cache optimization for best performance / quality tradeoff." )]
	public bool CacheAware = true;

	[Header( "Distance Fade" )]
	[Tooltip( "Control parameters at faraway." )]
	public bool FadeEnabled = false;

	[Tooltip( "Distance in Unity unities that start to fade." )]
	public float FadeStart = 100.0f;

	[Tooltip( "Length distance to performe the transition." )]
	public float FadeLength = 50.0f;

	[Tooltip( "Final Intensity parameter." )]
	[Range( 0, 1 )]
	public float FadeToIntensity = 0.0f;
	public Color FadeToTint = Color.black;

	[Tooltip( "Final Radius parameter." )]
	[Range( 0, 32 )]
	public float FadeToRadius = 2.0f;

	[Tooltip( "Final PowerExponent parameter." )]
	[Range( 0, 16 )]
	public float FadeToPowerExponent = 1.0f;

	[Tooltip( "Final Thickness parameter." )]
	[Range( 0, 1.0f )]
	public float FadeToThickness = 1.0f;

	[Header( "Bilateral Blur" )]
	public bool BlurEnabled = true;

	[Tooltip( "Radius in screen pixels." )]
	[Range( 1, 4 )]
	public int BlurRadius = 3;

	[Tooltip( "Number of times that the Blur will repeat." )]
	[Range( 1, 4 )]
	public int BlurPasses = 1;

	[Tooltip( "Sharpness of blur edge-detection: 0 = Softer Edges, 20 = Sharper Edges." )]
	[Range( 0, 20 )]
	public float BlurSharpness = 15.0f;

	[Header( "Temporal Filter" )]
	[Tooltip( "Accumulates the effect over the time." )]
	public bool FilterEnabled = true;

	[Tooltip( "Controls the accumulation decayment: 0 = More flicker with less ghosting, 1 = Less flicker with more ghosting." )]
	[Range( 0, 1 )]
	public float FilterBlending = 0.80f;

	[Tooltip( "Controls the discard sensitivity based on the motion of the scene and objects." )]
	[Range( 0, 1 )]
	public float FilterResponse = 0.50f;

	// Current state variables
	private bool m_HDR = true;
	private bool m_MSAA = true;

	// Previous state variables
	private PerPixelNormalSource m_prevPerPixelNormals;
	private ApplicationMethod m_prevApplyMethod;
	private bool m_prevDeferredReflections = false;
	private SampleCountLevel m_prevSampleCount = SampleCountLevel.Low;
	private bool m_prevDownsample = false;
	private bool m_prevCacheAware = false;
	private bool m_prevBlurEnabled = false;
	private int m_prevBlurRadius = 0;
	private int m_prevBlurPasses = 0;
	private bool m_prevFilterEnabled = true;
	private bool m_prevHDR = true;
	private bool m_prevMSAA = true;

#if UNITY_EDITOR
	private bool m_prevIsPlaying = false;
#endif

	private Camera m_targetCamera = null;

	private RenderTargetIdentifier[] applyDebugTargetsTemporal = new RenderTargetIdentifier[2];
	private RenderTargetIdentifier[] applyDeferredTargets_Log_Temporal = new RenderTargetIdentifier[3];
	private RenderTargetIdentifier[] applyDeferredTargetsTemporal = new RenderTargetIdentifier[3];
	private RenderTargetIdentifier[] applyOcclusionTemporal = new RenderTargetIdentifier[2];
	private RenderTargetIdentifier[] applyPostEffectTargetsTemporal = new RenderTargetIdentifier[2];

	// NOTE: MotionVectors are not supported in Deferred Injection mode due to 1 frame delay
	private bool UsingTemporalFilter { get { return ( m_sampleStep > 0 ) && ( FilterEnabled == true ) && ( m_targetCamera.cameraType != CameraType.SceneView ); } }
	private bool UsingMotionVectors { get { return UsingTemporalFilter && ( ApplyMethod != ApplicationMethod.Deferred ); } }

	private bool useMRTBlendingFallback = false;

	// Command buffer
	private struct CmdBuffer
	{
		public CommandBuffer cmdBuffer;
		public CameraEvent cmdBufferEvent;
		public string cmdBufferName;
	}

	CmdBuffer m_commandBuffer_Parameters;
	CmdBuffer m_commandBuffer_Occlusion;
	CmdBuffer m_commandBuffer_Apply;

	private void createCommandBuffer( ref CmdBuffer aCmdBuffer, string aCmdBufferName, CameraEvent aCameraEvent )
	{
		if( aCmdBuffer.cmdBuffer != null )
		{
			cleanupCommandBuffer( ref aCmdBuffer );
		}

		aCmdBuffer.cmdBufferName = aCmdBufferName;

		aCmdBuffer.cmdBuffer = new CommandBuffer();
		aCmdBuffer.cmdBuffer.name = aCmdBufferName;

		aCmdBuffer.cmdBufferEvent = aCameraEvent;

		m_targetCamera.AddCommandBuffer( aCameraEvent, aCmdBuffer.cmdBuffer );
	}

	private void cleanupCommandBuffer( ref CmdBuffer aCmdBuffer )
	{
		CommandBuffer[] currentCBs = m_targetCamera.GetCommandBuffers( aCmdBuffer.cmdBufferEvent );

		for( int i = 0; i < currentCBs.Length; i++ )
		{
			if( currentCBs[ i ].name == aCmdBuffer.cmdBufferName )
			{
				m_targetCamera.RemoveCommandBuffer( aCmdBuffer.cmdBufferEvent, currentCBs[ i ] );
			}
		}

		aCmdBuffer.cmdBufferName = null;
		aCmdBuffer.cmdBufferEvent = 0;
		aCmdBuffer.cmdBuffer = null;
	}

	// Quad Mesh
	static private Mesh m_quadMesh = null;

	private void createQuadMesh()
	{
		if( m_quadMesh == null )
		{
			m_quadMesh = new Mesh();
			m_quadMesh.vertices = new Vector3[ 4 ] { new Vector3( 0, 0, 0 ), new Vector3( 0, 1, 0 ), new Vector3( 1, 1, 0 ), new Vector3( 1, 0, 0 ) };
			m_quadMesh.uv = new Vector2[ 4 ] { new Vector2( 0, 0 ), new Vector2( 0, 1 ), new Vector2( 1, 1 ), new Vector2( 1, 0 ) };
			m_quadMesh.triangles = new int[ 6 ] { 0, 1, 2, 0, 2, 3 };

			m_quadMesh.normals = new Vector3[ 0 ];
			m_quadMesh.tangents = new Vector4[ 0 ];
			m_quadMesh.colors32 = new Color32[ 0 ];
			m_quadMesh.colors = new Color[ 0 ];
		}
	}


	void PerformBlit( CommandBuffer cb, Material mat, int pass )
	{
		cb.DrawMesh( m_quadMesh, Matrix4x4.identity, mat, 0, pass );
	}

	// Render Materials
	static private Material m_occlusionMat = null;
	static private Material m_blurMat = null;
	static private Material m_applyOcclusionMat = null;

	private void checkMaterials( bool aThroughErrorMsg )
	{
		if( m_occlusionMat == null )
		{
			m_occlusionMat = AmplifyOcclusionCommon.CreateMaterialWithShaderName( "Hidden/Amplify Occlusion/Occlusion", aThroughErrorMsg );
		}

		if( m_blurMat == null )
		{
			m_blurMat = AmplifyOcclusionCommon.CreateMaterialWithShaderName( "Hidden/Amplify Occlusion/Blur", aThroughErrorMsg );
		}

		if( m_applyOcclusionMat == null )
		{
			m_applyOcclusionMat = AmplifyOcclusionCommon.CreateMaterialWithShaderName( "Hidden/Amplify Occlusion/Apply", aThroughErrorMsg );

			if( m_applyOcclusionMat != null )
			{
				// some platforms still don't support MRT-blending; provide a fallback, if necessary
				useMRTBlendingFallback = m_applyOcclusionMat.GetTag( "MRTBlending", false ).ToUpper() != "TRUE";
			}
		}
	}

	private RenderTextureFormat m_occlusionRTFormat = RenderTextureFormat.RGHalf;
	private RenderTextureFormat m_accumTemporalRTFormat = RenderTextureFormat.ARGB32;
	private RenderTextureFormat m_temporaryEmissionRTFormat = RenderTextureFormat.ARGB2101010;
	private RenderTextureFormat m_motionIntensityRTFormat = RenderTextureFormat.R8;


	private bool checkRenderTextureFormats()
	{
		// test the two fallback formats first
		if( SystemInfo.SupportsRenderTextureFormat( RenderTextureFormat.ARGB32 ) && SystemInfo.SupportsRenderTextureFormat( RenderTextureFormat.ARGBHalf ) )
		{
			m_occlusionRTFormat = RenderTextureFormat.RGHalf;
			if( !SystemInfo.SupportsRenderTextureFormat( m_occlusionRTFormat ) )
			{
				m_occlusionRTFormat = RenderTextureFormat.RGFloat;
				if( !SystemInfo.SupportsRenderTextureFormat( m_occlusionRTFormat ) )
				{
					// already tested above
					m_occlusionRTFormat = RenderTextureFormat.ARGBHalf;
				}
			}

			return true;
		}
		return false;
	}


	void OnEnable()
	{
		m_myID = m_nextID;
		m_myIDstring = m_myID.ToString();
		m_nextID++;

		if( !checkRenderTextureFormats() )
		{
			Debug.LogError( "[AmplifyOcclusion] Target platform does not meet the minimum requirements for this effect to work properly." );

			this.enabled = false;

			return;
		}

		checkMaterials( false );
		createQuadMesh();
	}


	void Reset()
	{
		if( m_commandBuffer_Parameters.cmdBuffer != null )
		{
			cleanupCommandBuffer( ref m_commandBuffer_Parameters );
		}

		if( m_commandBuffer_Occlusion.cmdBuffer != null )
		{
			cleanupCommandBuffer( ref m_commandBuffer_Occlusion );
		}

		if( m_commandBuffer_Apply.cmdBuffer != null )
		{
			cleanupCommandBuffer( ref m_commandBuffer_Apply );
		}

		AmplifyOcclusionCommon.SafeReleaseRT( ref m_occlusionDepthRT );
		AmplifyOcclusionCommon.SafeReleaseRT( ref m_depthMipmap );
		releaseTemporalRT();

		m_tmpMipString = null;
	}

	void OnDisable()
	{
		Reset();
	}

	private void releaseTemporalRT()
	{
		if( m_temporalAccumRT != null )
		{
			for( int i = 0; i < m_temporalAccumRT.Length; i++ )
			{
				AmplifyOcclusionCommon.SafeReleaseRT( ref m_temporalAccumRT[ i ] );
			}
		}

		m_temporalAccumRT = null;
	}

	private bool m_paramsChanged = true;
	private bool m_clearHistory = true;

	private void ClearHistory( CommandBuffer cb )
	{
		m_clearHistory = false;

		if ( ( m_temporalAccumRT != null ) && ( m_occlusionDepthRT != null ) )
		{
			for( int i = 0; i < m_temporalAccumRT.Length; i++ )
			{
				cb.SetRenderTarget( m_temporalAccumRT[ i ] );
				PerformBlit( cb, m_occlusionMat, ShaderPass.ClearTemporal );
			}
		}
	}

	private void checkParamsChanged()
	{
		bool HDR = m_targetCamera.allowHDR; // && tier?
		bool MSAA = m_targetCamera.allowMSAA &&
					m_targetCamera.actualRenderingPath != RenderingPath.DeferredLighting &&
					m_targetCamera.actualRenderingPath != RenderingPath.DeferredShading &&
					QualitySettings.antiAliasing >= 1;

		int antiAliasing = MSAA ? QualitySettings.antiAliasing : 1;

		if( m_occlusionDepthRT != null )
		{
			if( ( m_occlusionDepthRT.width != m_target.width ) ||
				( m_occlusionDepthRT.height != m_target.height ) ||
				( m_prevMSAA != MSAA ) ||
				( !m_occlusionDepthRT.IsCreated() ) ||
				( m_temporalAccumRT != null && ( !m_temporalAccumRT[ 0 ].IsCreated() || !m_temporalAccumRT[ 1 ].IsCreated() ) )
#if UNITY_EDITOR
				|| ( ( m_prevIsPlaying == true ) && ( EditorApplication.isPlaying == false ) )
#endif
				)
			{
				AmplifyOcclusionCommon.SafeReleaseRT( ref m_occlusionDepthRT );
				AmplifyOcclusionCommon.SafeReleaseRT( ref m_depthMipmap );
				releaseTemporalRT();

				m_paramsChanged = true;
			}
			else
			{
				if( m_prevFilterEnabled != FilterEnabled )
				{
					releaseTemporalRT();
				}
			}


		}

		if( m_temporalAccumRT != null )
		{
			if( AmplifyOcclusionCommon.IsStereoMultiPassEnabled( m_targetCamera ) == true )
			{
				if( m_temporalAccumRT.Length != 4 )
				{
					m_temporalAccumRT = null;
				}
			}
			else
			{
				if( m_temporalAccumRT.Length != 2 )
				{
					m_temporalAccumRT = null;
				}
			}
		}

		if( m_occlusionDepthRT == null )
		{
			m_occlusionDepthRT = AmplifyOcclusionCommon.SafeAllocateRT( "_AO_OcclusionDepthTexture",
																		m_target.width,
																		m_target.height,
																		m_occlusionRTFormat,
																		RenderTextureReadWrite.Linear,
																		FilterMode.Bilinear );
		}

		if( m_temporalAccumRT == null && FilterEnabled )
		{
			if( AmplifyOcclusionCommon.IsStereoMultiPassEnabled( m_targetCamera ) == true )
			{
				m_temporalAccumRT = new RenderTexture[ 4 ];
			}
			else
			{
				m_temporalAccumRT = new RenderTexture[ 2 ];
			}

			for( int i = 0; i < m_temporalAccumRT.Length; i++ )
			{
				m_temporalAccumRT[ i ] = AmplifyOcclusionCommon.SafeAllocateRT( "_AO_TemporalAccum_" + i.ToString(),
																				m_target.width,
																				m_target.height,
																				m_accumTemporalRTFormat,
																				RenderTextureReadWrite.Linear,
																				FilterMode.Bilinear,
																				antiAliasing );
			}

			m_clearHistory = true;
		}

		if( ( CacheAware == true ) && ( m_depthMipmap == null ) )
		{
			m_depthMipmap = AmplifyOcclusionCommon.SafeAllocateRT( "_AO_DepthMipmap",
																	m_target.width >> 1,
																	m_target.height >> 1,
																	RenderTextureFormat.RFloat,
																	RenderTextureReadWrite.Linear,
																	FilterMode.Point,
																	1,
																	true );

			int minSize = (int)Mathf.Min( m_target.width, m_target.height );
			m_numberMips = (int)( Mathf.Log( (float)minSize, 2.0f ) + 1.0f ) - 1;

			m_tmpMipString = null;
			m_tmpMipString = new string[m_numberMips];

			for( int i = 0; i < m_numberMips; i++ )
			{
				m_tmpMipString[i] = "_AO_TmpMip_" + i.ToString();
			}
		}
		else
		{
			if( ( CacheAware == false ) && ( m_depthMipmap != null ) )
			{
				AmplifyOcclusionCommon.SafeReleaseRT( ref m_depthMipmap );
				m_tmpMipString = null;
			}
		}

		if( ( m_prevSampleCount != SampleCount ) ||
			( m_prevDownsample != Downsample ) ||
			( m_prevCacheAware != CacheAware ) ||
			( m_prevBlurEnabled != BlurEnabled ) ||
			( ( ( m_prevBlurPasses != BlurPasses ) ||
			    ( m_prevBlurRadius != BlurRadius ) ) && ( BlurEnabled == true ) ) ||
			( m_prevFilterEnabled != FilterEnabled ) ||
			( m_prevHDR != HDR ) ||
			( m_prevMSAA != MSAA ) )
		{
			m_clearHistory |= ( m_prevHDR != HDR );
			m_clearHistory |= ( m_prevMSAA != MSAA );

			m_HDR = HDR;
			m_MSAA = MSAA;

			m_paramsChanged = true;
		}

#if UNITY_EDITOR
		m_prevIsPlaying = EditorApplication.isPlaying;
#endif
	}


	private void updateParams()
	{
		m_prevSampleCount = SampleCount;
		m_prevDownsample = Downsample;
		m_prevCacheAware = CacheAware;
		m_prevBlurEnabled = BlurEnabled;
		m_prevBlurPasses = BlurPasses;
		m_prevBlurRadius = BlurRadius;
		m_prevFilterEnabled = FilterEnabled;
		m_prevHDR = m_HDR;
		m_prevMSAA = m_MSAA;

		m_paramsChanged = false;
	}

	void Update()
	{
		if( m_targetCamera != null )
		{
			if( m_targetCamera.actualRenderingPath != RenderingPath.DeferredShading )
			{
				if( PerPixelNormals != PerPixelNormalSource.None && PerPixelNormals != PerPixelNormalSource.Camera )
				{
					m_paramsChanged = true;
					PerPixelNormals = PerPixelNormalSource.Camera;
					UnityEngine.Debug.LogWarning( "[AmplifyOcclusion] GBuffer Normals only available in Camera Deferred Shading mode. Switched to Camera source." );
				}

				if( ApplyMethod == ApplicationMethod.Deferred )
				{
					m_paramsChanged = true;
					ApplyMethod = ApplicationMethod.PostEffect;
					UnityEngine.Debug.LogWarning( "[AmplifyOcclusion] Deferred Method requires a Deferred Shading path. Switching to Post Effect Method." );
				}
			}
			else
			{
				if( PerPixelNormals == PerPixelNormalSource.Camera )
				{
					m_paramsChanged = true;
					PerPixelNormals = PerPixelNormalSource.GBuffer;
					UnityEngine.Debug.LogWarning( "[AmplifyOcclusion] Camera Normals not supported for Deferred Method. Switching to GBuffer Normals." );
				}
			}

			if( ( m_targetCamera.depthTextureMode & DepthTextureMode.Depth ) == 0 )
			{
				m_targetCamera.depthTextureMode |= DepthTextureMode.Depth;
			}

			if( ( PerPixelNormals == PerPixelNormalSource.Camera ) &&
					( m_targetCamera.depthTextureMode & DepthTextureMode.DepthNormals ) == 0 )
			{
				m_targetCamera.depthTextureMode |= DepthTextureMode.DepthNormals;
			}

			if( ( UsingMotionVectors == true ) &&
				( m_targetCamera.depthTextureMode & DepthTextureMode.MotionVectors ) == 0 )
			{
				m_targetCamera.depthTextureMode |= DepthTextureMode.MotionVectors;
			}

		}
		else
		{
			m_targetCamera = GetComponent<Camera>();
		}
	}


	void OnPreRender()
	{
		Profiler.BeginSample( "AO - OnPreRender" );

		checkMaterials( true );

		if( m_targetCamera != null )
		{
			#if UNITY_EDITOR
			if( ( m_targetCamera.cameraType == CameraType.SceneView ) &&
				( ( ( PerPixelNormals == PerPixelNormalSource.GBuffer ) && ( m_targetCamera.orthographic == true ) ) ||
				  ( PerPixelNormals == PerPixelNormalSource.Camera ) && ( SceneView.lastActiveSceneView.m_SceneLighting == false ) ) )
			{
				PerPixelNormals = PerPixelNormalSource.None;
			}
			#endif

			bool deferredReflections = ( GraphicsSettings.GetShaderMode( BuiltinShaderType.DeferredReflections ) != BuiltinShaderMode.Disabled );

			if( ( m_prevPerPixelNormals != PerPixelNormals ) ||
				( m_prevApplyMethod != ApplyMethod ) ||
				( m_prevDeferredReflections != deferredReflections ) ||
				( m_commandBuffer_Parameters.cmdBuffer == null ) ||
				( m_commandBuffer_Occlusion.cmdBuffer == null ) ||
				( m_commandBuffer_Apply.cmdBuffer == null )
				)
			{
				CameraEvent cameraStage = CameraEvent.BeforeImageEffectsOpaque;
				if( ApplyMethod == ApplicationMethod.Deferred )
				{
					cameraStage = deferredReflections ? CameraEvent.BeforeReflections : CameraEvent.BeforeLighting;
				}

				createCommandBuffer( ref m_commandBuffer_Parameters, "AmplifyOcclusion_Parameters_" + m_myIDstring, cameraStage );
				createCommandBuffer( ref m_commandBuffer_Occlusion, "AmplifyOcclusion_Compute_" + m_myIDstring, cameraStage );
				createCommandBuffer( ref m_commandBuffer_Apply, "AmplifyOcclusion_Apply_" + m_myIDstring, cameraStage );

				m_prevPerPixelNormals = PerPixelNormals;
				m_prevApplyMethod = ApplyMethod;
				m_prevDeferredReflections = deferredReflections;

				m_paramsChanged = true;
			}

			if( ( m_commandBuffer_Parameters.cmdBuffer != null ) &&
				( m_commandBuffer_Occlusion.cmdBuffer != null ) &&
				( m_commandBuffer_Apply.cmdBuffer != null ) )
			{
				if( AmplifyOcclusionCommon.IsStereoMultiPassEnabled( m_targetCamera ) == true )
				{
					uint curStepIdx = ( m_sampleStep >> 1 ) & 1;
					uint curEyeIdx = ( m_sampleStep & 1 );
					m_curTemporalIdx  = ( curEyeIdx * 2 ) + ( 0 + curStepIdx );
					m_prevTemporalIdx = ( curEyeIdx * 2 ) + ( 1 - curStepIdx );
				}
				else
				{
					uint curStepIdx = m_sampleStep & 1;
					m_curTemporalIdx  = 0 + curStepIdx;
					m_prevTemporalIdx = 1 - curStepIdx;
				}

				m_commandBuffer_Parameters.cmdBuffer.Clear();

				UpdateGlobalShaderConstants( m_commandBuffer_Parameters.cmdBuffer );

				UpdateGlobalShaderConstants_Matrices( m_commandBuffer_Parameters.cmdBuffer );

				UpdateGlobalShaderConstants_AmbientOcclusion( m_commandBuffer_Parameters.cmdBuffer );

				checkParamsChanged();

				if( m_paramsChanged )
				{
					m_commandBuffer_Occlusion.cmdBuffer.Clear();

					commandBuffer_FillComputeOcclusion( m_commandBuffer_Occlusion.cmdBuffer );
				}

				m_commandBuffer_Apply.cmdBuffer.Clear();

				if( ApplyMethod == ApplicationMethod.Debug )
				{
					commandBuffer_FillApplyDebug( m_commandBuffer_Apply.cmdBuffer );
				}
				else
				{
					if( ApplyMethod == ApplicationMethod.PostEffect )
					{
						commandBuffer_FillApplyPostEffect( m_commandBuffer_Apply.cmdBuffer );
					}
					else
					{
						bool logTarget = !m_HDR;

						commandBuffer_FillApplyDeferred( m_commandBuffer_Apply.cmdBuffer, logTarget );
					}
				}

				updateParams();

				m_sampleStep++; // No clamp, free running counter
			}
		}
		else
		{
			m_targetCamera = GetComponent<Camera>();

			Update();

			#if UNITY_EDITOR
			if( m_targetCamera.cameraType != CameraType.SceneView )
			{
				System.Type T = System.Type.GetType( "UnityEditor.GameView,UnityEditor" );
				UnityEngine.Object[] array = Resources.FindObjectsOfTypeAll( T );

				if( array.Length > 0 )
				{
					EditorWindow gameView = (EditorWindow)array[ 0 ];
					gameView.Focus();
				}
			}
			#endif
		}

		Profiler.EndSample();
	}


	void OnPostRender()
	{
		if( m_occlusionDepthRT != null )
		{
			m_occlusionDepthRT.MarkRestoreExpected();
		}
		if( m_temporalAccumRT != null )
		{
			foreach ( var rt in m_temporalAccumRT )
			{
				rt.MarkRestoreExpected();
			}
		}
	}


	private RenderTexture m_occlusionDepthRT = null;
	private RenderTexture[] m_temporalAccumRT = null;
	private RenderTexture m_depthMipmap = null;

	private uint m_sampleStep = 0;
	private uint m_curTemporalIdx = 0;
	private uint m_prevTemporalIdx = 0;

	private string[] m_tmpMipString = null;
	private int m_numberMips = 0;
	private void commandBuffer_FillComputeOcclusion( CommandBuffer cb )
	{
		cb.BeginSample( "AO 1 - ComputeOcclusion" );

		if( ( PerPixelNormals == PerPixelNormalSource.GBuffer ) ||
			( PerPixelNormals == PerPixelNormalSource.GBufferOctaEncoded ) )
		{
			cb.SetGlobalTexture( PropertyID._AO_GBufferNormals, BuiltinRenderTextureType.GBuffer2 );
		}

		Vector4 oneOverSize_Size = new Vector4( m_target.oneOverWidth,
												m_target.oneOverHeight,
												m_target.width,
												m_target.height );

		int sampleCountPass = ( (int)SampleCount ) * AmplifyOcclusionCommon.PerPixelNormalSourceCount;

		int occlusionPass = ( ShaderPass.OcclusionLow_None +
								sampleCountPass +
								( (int)PerPixelNormals ) );

		if( CacheAware == true )
		{
			occlusionPass += ShaderPass.OcclusionLow_None_UseDynamicDepthMips;

			// Construct Depth mipmaps
			int previouslyTmpMipRT = 0;

			for( int i = 0; i < m_numberMips; i++ )
			{
				int tmpMipRT;

				int width = m_target.width >> ( i + 1 );
				int height = m_target.height >> ( i + 1 );

				tmpMipRT = AmplifyOcclusionCommon.SafeAllocateTemporaryRT( cb, m_tmpMipString[ i ],
																			width, height,
																			RenderTextureFormat.RFloat,
																			RenderTextureReadWrite.Linear,
																			FilterMode.Bilinear );

				// _AO_CurrDepthSource was previously set
				cb.SetRenderTarget( tmpMipRT );

				PerformBlit( cb, m_occlusionMat, ( ( i == 0 )?ShaderPass.ScaleDownCloserDepthEven_CameraDepthTexture:ShaderPass.ScaleDownCloserDepthEven ) );

				cb.CopyTexture( tmpMipRT, 0, 0, m_depthMipmap, 0, i );

				if( previouslyTmpMipRT != 0 )
				{
					AmplifyOcclusionCommon.SafeReleaseTemporaryRT( cb, previouslyTmpMipRT );
				}

				previouslyTmpMipRT = tmpMipRT;

				cb.SetGlobalTexture( PropertyID._AO_CurrDepthSource, tmpMipRT ); // Set next MipRT ID
			}

			AmplifyOcclusionCommon.SafeReleaseTemporaryRT( cb, previouslyTmpMipRT );

			cb.SetGlobalTexture( PropertyID._AO_SourceDepthMipmap, m_depthMipmap );
		}

		if( Downsample )
		{
			int halfWidth = m_target.width / 2;
			int halfHeight = m_target.height / 2;

			int tmpSmallOcclusionRT = AmplifyOcclusionCommon.SafeAllocateTemporaryRT( cb, "_AO_SmallOcclusionTexture",
																halfWidth, halfHeight,
																m_occlusionRTFormat,
																RenderTextureReadWrite.Linear,
																FilterMode.Bilinear );

			cb.SetGlobalVector( PropertyID._AO_Source_TexelSize, oneOverSize_Size );
			cb.SetGlobalVector( PropertyID._AO_Target_TexelSize, new Vector4( 1.0f / ( m_target.width / 2.0f ),
																			1.0f / ( m_target.height / 2.0f ),
																			m_target.width / 2.0f,
																			m_target.height / 2.0f ) );

			cb.SetRenderTarget( tmpSmallOcclusionRT );
			PerformBlit( cb, m_occlusionMat, occlusionPass );

			cb.SetRenderTarget( default( RenderTexture ) );
			cb.EndSample( "AO 1 - ComputeOcclusion" );

			if( BlurEnabled == true )
			{
				commandBuffer_Blur( cb, tmpSmallOcclusionRT, halfWidth, halfHeight );
			}

			// Combine
			cb.BeginSample( "AO 2b - Combine" );

			cb.SetGlobalTexture( PropertyID._AO_CurrOcclusionDepth, tmpSmallOcclusionRT );

			cb.SetGlobalVector( PropertyID._AO_Target_TexelSize, oneOverSize_Size );

			cb.SetRenderTarget( m_occlusionDepthRT );

			PerformBlit( cb, m_occlusionMat, ShaderPass.CombineDownsampledOcclusionDepth );

			AmplifyOcclusionCommon.SafeReleaseTemporaryRT( cb, tmpSmallOcclusionRT );

			cb.SetRenderTarget( default( RenderTexture ) );
			cb.EndSample( "AO 2b - Combine" );
		}
		else
		{
			cb.SetGlobalVector( PropertyID._AO_Source_TexelSize, oneOverSize_Size );
			cb.SetGlobalVector( PropertyID._AO_Target_TexelSize, oneOverSize_Size );

			cb.SetRenderTarget( m_occlusionDepthRT );
			PerformBlit( cb, m_occlusionMat, occlusionPass );

			cb.SetRenderTarget( default( RenderTexture ) );
			cb.EndSample( "AO 1 - ComputeOcclusion" );

			if( BlurEnabled == true )
			{
				commandBuffer_Blur( cb, m_occlusionDepthRT, m_target.width, m_target.height );
			}
		}
	}


	int commandBuffer_NeighborMotionIntensity( CommandBuffer cb, int aSourceWidth, int aSourceHeight )
	{
		int tmpRT = AmplifyOcclusionCommon.SafeAllocateTemporaryRT( cb, "_AO_IntensityTmp",
																	aSourceWidth / 4, aSourceHeight / 4,
																	m_motionIntensityRTFormat,
																	RenderTextureReadWrite.Linear,
																	FilterMode.Bilinear );


		cb.SetRenderTarget( tmpRT );
		cb.SetGlobalVector( "_AO_Target_TexelSize", new Vector4( 1.0f / ( aSourceWidth / 4.0f ),
																 1.0f / ( aSourceHeight / 4.0f ),
																 aSourceWidth / 4.0f,
																 aSourceHeight / 4.0f ) );


		PerformBlit( cb, m_occlusionMat, ShaderPass.NeighborMotionIntensity );

		int tmpBlurRT = AmplifyOcclusionCommon.SafeAllocateTemporaryRT( cb, "_AO_BlurIntensityTmp",
																		aSourceWidth / 4, aSourceHeight / 4,
																		m_motionIntensityRTFormat,
																		RenderTextureReadWrite.Linear,
																		FilterMode.Bilinear );

		// Horizontal
		cb.SetGlobalTexture( PropertyID._AO_CurrMotionIntensity, tmpRT );
		cb.SetRenderTarget( tmpBlurRT );
		PerformBlit( cb, m_blurMat, ShaderPass.BlurHorizontalIntensity );

		// Vertical
		cb.SetGlobalTexture( PropertyID._AO_CurrMotionIntensity, tmpBlurRT );
		cb.SetRenderTarget( tmpRT );
		PerformBlit( cb, m_blurMat, ShaderPass.BlurVerticalIntensity );

		AmplifyOcclusionCommon.SafeReleaseTemporaryRT( cb, tmpBlurRT );

		cb.SetGlobalTexture( PropertyID._AO_CurrMotionIntensity, tmpRT );

		return tmpRT;
	}


	void commandBuffer_Blur( CommandBuffer cb, RenderTargetIdentifier aSourceRT, int aSourceWidth, int aSourceHeight )
	{
		cb.BeginSample( "AO 2 - Blur" );

		int tmpBlurRT = AmplifyOcclusionCommon.SafeAllocateTemporaryRT( cb, "_AO_BlurTmp",
																		aSourceWidth, aSourceHeight,
																		m_occlusionRTFormat,
																		RenderTextureReadWrite.Linear,
																		FilterMode.Bilinear );

		// Apply Cross Bilateral Blur
		for( int i = 0; i < BlurPasses; i++ )
		{
			// Horizontal
			cb.SetGlobalTexture( PropertyID._AO_CurrOcclusionDepth, aSourceRT );

			int blurHorizontalPass = ShaderPass.BlurHorizontal1 + ( BlurRadius - 1 ) * 2;

			cb.SetRenderTarget( tmpBlurRT );

			PerformBlit( cb, m_blurMat, blurHorizontalPass );


			// Vertical
			cb.SetGlobalTexture( PropertyID._AO_CurrOcclusionDepth, tmpBlurRT );

			int blurVerticalPass = ShaderPass.BlurVertical1 + ( BlurRadius - 1 ) * 2;

			cb.SetRenderTarget( aSourceRT );

			PerformBlit( cb, m_blurMat, blurVerticalPass );
		}

		AmplifyOcclusionCommon.SafeReleaseTemporaryRT( cb, tmpBlurRT );

		cb.SetRenderTarget( default( RenderTexture ) );
		cb.EndSample( "AO 2 - Blur" );
	}

	int getTemporalPass()
	{
		return ( ( ( UsingMotionVectors == true ) && ( m_sampleStep > 1 ) ) ? ( 1 << 0 ) : 0 );
	}

	void commandBuffer_TemporalFilter( CommandBuffer cb )
	{
		if( m_clearHistory == true )
		{
			ClearHistory( cb );
		}

		// Temporal Filter
		float temporalAdj = Mathf.Lerp( 0.01f, 0.99f, FilterBlending );

		cb.SetGlobalFloat( PropertyID._AO_TemporalCurveAdj, temporalAdj );
		cb.SetGlobalFloat( PropertyID._AO_TemporalMotionSensibility, FilterResponse * FilterResponse + 0.01f );

		cb.SetGlobalTexture( PropertyID._AO_CurrOcclusionDepth, m_occlusionDepthRT );
		cb.SetGlobalTexture( PropertyID._AO_TemporalAccumm, m_temporalAccumRT[ m_prevTemporalIdx ] );
	}

	private readonly RenderTargetIdentifier[] m_applyDeferredTargets =
	{
		BuiltinRenderTextureType.GBuffer0,		// RGB: Albedo, A: Occ
		BuiltinRenderTextureType.CameraTarget,	// RGB: Emission, A: None
	};

	private readonly RenderTargetIdentifier[] m_applyDeferredTargets_Log =
	{
		BuiltinRenderTextureType.GBuffer0,		// RGB: Albedo, A: Occ
		BuiltinRenderTextureType.GBuffer3		// RGB: Emission, A: None
	};

	void commandBuffer_FillApplyDeferred( CommandBuffer cb, bool logTarget )
	{
		cb.BeginSample( "AO 3 - ApplyDeferred" );

		if( !logTarget )
		{
			if( UsingTemporalFilter )
			{
				commandBuffer_TemporalFilter( cb );

				int tmpMotionIntensityRT = 0;

				if( UsingMotionVectors == true )
				{
					tmpMotionIntensityRT = commandBuffer_NeighborMotionIntensity( cb, m_target.width, m_target.height );
				}

				int applyOcclusionRT = 0;
				if ( useMRTBlendingFallback )
				{
					applyOcclusionRT = AmplifyOcclusionCommon.SafeAllocateTemporaryRT( cb, "_AO_ApplyOcclusionTexture", m_target.fullWidth, m_target.fullHeight, RenderTextureFormat.ARGB32 );

					applyOcclusionTemporal[0] = applyOcclusionRT;
					applyOcclusionTemporal[1] = new RenderTargetIdentifier( m_temporalAccumRT[ m_curTemporalIdx ] );

					cb.SetRenderTarget( applyOcclusionTemporal, applyOcclusionTemporal[ 0 ] /* Not used, just to make Unity happy */ );
					PerformBlit( cb, m_applyOcclusionMat, ShaderPass.ApplyPostEffectTemporal + getTemporalPass() ); // re-use ApplyPostEffectTemporal pass to apply without Blend to the RT.
				}
				else
				{
					applyDeferredTargetsTemporal[0] = m_applyDeferredTargets[0];
					applyDeferredTargetsTemporal[1] = m_applyDeferredTargets[1];
					applyDeferredTargetsTemporal[2] = new RenderTargetIdentifier( m_temporalAccumRT[ m_curTemporalIdx ] );

					cb.SetRenderTarget( applyDeferredTargetsTemporal, applyDeferredTargetsTemporal[ 0 ] /* Not used, just to make Unity happy */ );
					PerformBlit( cb, m_applyOcclusionMat, ShaderPass.ApplyDeferredTemporal + getTemporalPass() );
				}

				if ( useMRTBlendingFallback )
				{
					cb.SetGlobalTexture( "_AO_ApplyOcclusionTexture", applyOcclusionRT );

					applyOcclusionTemporal[0] = m_applyDeferredTargets[0];
					applyOcclusionTemporal[1] = m_applyDeferredTargets[1];

					cb.SetRenderTarget( applyOcclusionTemporal, applyOcclusionTemporal[ 0 ] /* Not used, just to make Unity happy */ );
					PerformBlit( cb, m_applyOcclusionMat, ShaderPass.ApplyDeferredTemporalMultiply );

					AmplifyOcclusionCommon.SafeReleaseTemporaryRT( cb, applyOcclusionRT );
				}

				if( UsingMotionVectors == true )
				{
					AmplifyOcclusionCommon.SafeReleaseTemporaryRT( cb, tmpMotionIntensityRT );
				}
			}
			else
			{
				cb.SetGlobalTexture( PropertyID._AO_CurrOcclusionDepth, m_occlusionDepthRT );

				// Multiply Occlusion
				cb.SetRenderTarget( m_applyDeferredTargets, m_applyDeferredTargets[ 0 ] /* Not used, just to make Unity happy */ );
				PerformBlit( cb, m_applyOcclusionMat, ShaderPass.ApplyDeferred );
			}
		}
		else
		{
			// Copy Albedo and Emission to temporary buffers
			int gbufferAlbedoRT = AmplifyOcclusionCommon.SafeAllocateTemporaryRT( cb, "_AO_tmpAlbedo",
																					m_target.fullWidth, m_target.fullHeight,
																					RenderTextureFormat.ARGB32 );

			int gbufferEmissionRT = AmplifyOcclusionCommon.SafeAllocateTemporaryRT( cb, "_AO_tmpEmission",
																					m_target.fullWidth, m_target.fullHeight,
																					m_temporaryEmissionRTFormat );

			cb.Blit( BuiltinRenderTextureType.GBuffer0, gbufferAlbedoRT );
			cb.Blit( BuiltinRenderTextureType.GBuffer3, gbufferEmissionRT );

			cb.SetGlobalTexture( PropertyID._AO_GBufferAlbedo, gbufferAlbedoRT );
			cb.SetGlobalTexture( PropertyID._AO_GBufferEmission, gbufferEmissionRT );

			if( UsingTemporalFilter )
			{
				commandBuffer_TemporalFilter( cb );

				int tmpMotionIntensityRT = 0;

				if( UsingMotionVectors == true )
				{
					tmpMotionIntensityRT = commandBuffer_NeighborMotionIntensity( cb, m_target.width, m_target.height );
				}

				applyDeferredTargets_Log_Temporal[0] = m_applyDeferredTargets_Log[0];
				applyDeferredTargets_Log_Temporal[1] = m_applyDeferredTargets_Log[1];
				applyDeferredTargets_Log_Temporal[2] = new RenderTargetIdentifier( m_temporalAccumRT[ m_curTemporalIdx ] );

				cb.SetRenderTarget( applyDeferredTargets_Log_Temporal, applyDeferredTargets_Log_Temporal[ 0 ] /* Not used, just to make Unity happy */ );
				PerformBlit( cb, m_applyOcclusionMat, ShaderPass.ApplyDeferredLogTemporal + getTemporalPass() );

				if( UsingMotionVectors == true )
				{
					AmplifyOcclusionCommon.SafeReleaseTemporaryRT( cb, tmpMotionIntensityRT );
				}
			}
			else
			{
				cb.SetGlobalTexture( PropertyID._AO_CurrOcclusionDepth, m_occlusionDepthRT );

				cb.SetRenderTarget( m_applyDeferredTargets_Log, m_applyDeferredTargets_Log[ 0 ] /* Not used, just to make Unity happy */ );
				PerformBlit( cb, m_applyOcclusionMat, ShaderPass.ApplyDeferredLog );
			}

			AmplifyOcclusionCommon.SafeReleaseTemporaryRT( cb, gbufferAlbedoRT );
			AmplifyOcclusionCommon.SafeReleaseTemporaryRT( cb, gbufferEmissionRT );
		}

		cb.SetRenderTarget( default( RenderTexture ) );
		cb.EndSample( "AO 3 - ApplyDeferred" );
	}


	void commandBuffer_FillApplyPostEffect( CommandBuffer cb )
	{
		cb.BeginSample( "AO 3 - ApplyPostEffect" );

		if( UsingTemporalFilter )
		{
			commandBuffer_TemporalFilter( cb );

			int tmpMotionIntensityRT = 0;

			if( UsingMotionVectors == true )
			{
				tmpMotionIntensityRT = commandBuffer_NeighborMotionIntensity( cb, m_target.width, m_target.height );
			}

			int applyOcclusionRT = 0;
			if ( useMRTBlendingFallback )
			{
				applyOcclusionRT = AmplifyOcclusionCommon.SafeAllocateTemporaryRT( cb, "_AO_ApplyOcclusionTexture", m_target.fullWidth, m_target.fullHeight, RenderTextureFormat.ARGB32 );
				applyPostEffectTargetsTemporal[ 0 ] = applyOcclusionRT;
			}
			else
			{
				applyPostEffectTargetsTemporal[ 0 ] = BuiltinRenderTextureType.CameraTarget;
			}

			applyPostEffectTargetsTemporal[1] = new RenderTargetIdentifier( m_temporalAccumRT[ m_curTemporalIdx ] );

			cb.SetRenderTarget( applyPostEffectTargetsTemporal, applyPostEffectTargetsTemporal[ 0 ] /* Not used, just to make Unity happy */ );
			PerformBlit( cb, m_applyOcclusionMat, ShaderPass.ApplyPostEffectTemporal + getTemporalPass() );

			if ( useMRTBlendingFallback )
			{
				cb.SetGlobalTexture( "_AO_ApplyOcclusionTexture", applyOcclusionRT );

				cb.SetRenderTarget( BuiltinRenderTextureType.CameraTarget );
				PerformBlit( cb, m_applyOcclusionMat, ShaderPass.ApplyPostEffectTemporalMultiply );

				AmplifyOcclusionCommon.SafeReleaseTemporaryRT( cb, applyOcclusionRT );
			}

			if( UsingMotionVectors == true )
			{
				AmplifyOcclusionCommon.SafeReleaseTemporaryRT( cb, tmpMotionIntensityRT );
			}
		}
		else
		{
			cb.SetGlobalTexture( PropertyID._AO_CurrOcclusionDepth, m_occlusionDepthRT );

			cb.SetRenderTarget( BuiltinRenderTextureType.CameraTarget );
			PerformBlit( cb, m_applyOcclusionMat, ShaderPass.ApplyPostEffect );
		}

		cb.SetRenderTarget( default( RenderTexture ) );
		cb.EndSample( "AO 3 - ApplyPostEffect" );
	}


	void commandBuffer_FillApplyDebug( CommandBuffer cb )
	{
		cb.BeginSample( "AO 3 - ApplyDebug" );

		if( UsingTemporalFilter )
		{
			commandBuffer_TemporalFilter( cb );

			int tmpMotionIntensityRT = 0;

			if( UsingMotionVectors == true )
			{
				tmpMotionIntensityRT = commandBuffer_NeighborMotionIntensity( cb, m_target.width, m_target.height );
			}

			applyDebugTargetsTemporal[0] = BuiltinRenderTextureType.CameraTarget;
			applyDebugTargetsTemporal[1] = new RenderTargetIdentifier( m_temporalAccumRT[ m_curTemporalIdx ] );

			cb.SetRenderTarget( applyDebugTargetsTemporal, applyDebugTargetsTemporal[ 0 ] /* Not used, just to make Unity happy */ );
			PerformBlit( cb, m_applyOcclusionMat, ShaderPass.ApplyDebugTemporal + getTemporalPass() );

			if( UsingMotionVectors == true )
			{
				AmplifyOcclusionCommon.SafeReleaseTemporaryRT( cb, tmpMotionIntensityRT );
			}
		}
		else
		{
			cb.SetGlobalTexture( PropertyID._AO_CurrOcclusionDepth, m_occlusionDepthRT );

			cb.SetRenderTarget( BuiltinRenderTextureType.CameraTarget );
			PerformBlit( cb, m_applyOcclusionMat, ShaderPass.ApplyDebug );
		}

		cb.SetRenderTarget( default( RenderTexture ) );
		cb.EndSample( "AO 3 - ApplyDebug" );
	}

	private TargetDesc m_target = new TargetDesc();

	void UpdateGlobalShaderConstants( CommandBuffer cb )
	{
		AmplifyOcclusionCommon.UpdateGlobalShaderConstants( cb, ref m_target, m_targetCamera, Downsample );
	}

	void UpdateGlobalShaderConstants_AmbientOcclusion( CommandBuffer cb )
	{
		// Ambient Occlusion
		cb.SetGlobalFloat( PropertyID._AO_Radius, Radius );
		cb.SetGlobalFloat( PropertyID._AO_PowExponent, PowerExponent );
		cb.SetGlobalFloat( PropertyID._AO_Bias, Bias * Bias );
		cb.SetGlobalColor( PropertyID._AO_Levels, new Color( Tint.r, Tint.g, Tint.b, Intensity ) );

		float invThickness = ( 1.0f - Thickness );
		cb.SetGlobalFloat( PropertyID._AO_ThicknessDecay, ( 1.0f - invThickness * invThickness ) * 0.98f );

		if( BlurEnabled == true )
		{
			cb.SetGlobalFloat( PropertyID._AO_BlurSharpness, BlurSharpness * 100 );
		}

		// Distance Fade
		if( FadeEnabled == true )
		{
			FadeStart = Mathf.Max( 0.0f, FadeStart );
			FadeLength = Mathf.Max( 0.01f, FadeLength );

			float rcpFadeLength = 1.0f / FadeLength;

			cb.SetGlobalVector( PropertyID._AO_FadeParams, new Vector2( FadeStart, rcpFadeLength ) );
			float invFadeThickness = ( 1.0f - FadeToThickness );
			cb.SetGlobalVector( PropertyID._AO_FadeValues, new Vector4( FadeToIntensity, FadeToRadius, FadeToPowerExponent, ( 1.0f - invFadeThickness * invFadeThickness ) * 0.98f ) );
			cb.SetGlobalColor( PropertyID._AO_FadeToTint, new Color( FadeToTint.r, FadeToTint.g, FadeToTint.b, 0.0f ) );
		}
		else
		{
			cb.SetGlobalVector( PropertyID._AO_FadeParams, new Vector2( 0.0f, 0.0f ) );
		}

		if( FilterEnabled == true )
		{
			AmplifyOcclusionCommon.CommandBuffer_TemporalFilterDirectionsOffsets( cb, m_sampleStep );
		}
		else
		{
			cb.SetGlobalFloat( PropertyID._AO_TemporalDirections, 0 );
			cb.SetGlobalFloat( PropertyID._AO_TemporalOffsets, 0 );
		}
	}

	AmplifyOcclusionViewProjMatrix m_viewProjMatrix = new AmplifyOcclusionViewProjMatrix();
	void UpdateGlobalShaderConstants_Matrices( CommandBuffer cb )
	{
		m_viewProjMatrix.UpdateGlobalShaderConstants_Matrices( cb, m_targetCamera, UsingTemporalFilter );
	}
}
