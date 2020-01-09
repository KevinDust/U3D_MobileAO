About

  Amplify Occlusion 2 (c) Amplify Creations, Lda. All rights reserved.

  Amplify Occlusion 2 is a Robust Ambient Occlusion extension for Unity
	 
  Redistribution of Amplify Occlusion is frowned upon. If you want to share the 
  software, please refer others to the official download page:

    http://amplify.pt/unity/amplify-occlusion
	
Description

  Amplify Occlusion offers an efficient way to simulate realistic shadowing around 
  objects, it can even account for light occlusion allowing for non-uniform shadows
  providing additional depth to scenes. It takes the best out of fast performing SSAO,
  the accurate and flexible results provided by approximate obscurance-based techniques
  and combines it into a all-in-one flexible Unity package.
	
Features

  * Based on Ground Truth Ambient Occlusion
  * PS4, Xbox One and Switch compatible
  * VR Single and Multi-pass support
  * Up to 2X faster than Amplify Occlusion 1.0
  * Revamped Spatial and Temporal Filters
  * Dramatically Higher-Quality
  * Higher Flexibility
  * Under 1 ms on a mid-range GPU at Full HD
  * Accurate and fast-performing
  * Deferred and Forward Rendering
  * PBR compatible injection mode
  * Superior occlusion approximation
  * Extensive blur and intensity controls

Supported Platforms

  * All platforms 

Minimum Requirements

  Software

    Unity 5.6.0+

Quick Guide
  
  1) Select and apply “Image Effects/Amplify Occlusion” to your main camera.
  2) Adjust the Intensity and Radius.
  3) Adjust the blur values until you are satisfied with the results.

Scriptable Render Pipeline How-to

  First go to "Assets/Import Package/Custom Package..." and then
  select "Assets/AmplifyOcclusion/Packages/PostProcessingSRP_XXX.unitypackage"

  How to set up an SRP project example:

  1) Create SRP asset via Assets menu:

       Create/Rendering/High Definition Render Pipeline Asset

       OR

       Create/Rendering/Lightweight Render Pipeline Asset

  2) Set Edit->ProjectSettings/Player/Other settings/ColorSpace to Linear (necessary for HD SRP)
  3) Edit->ProjectSettings/Graphics/Scriptable Render Pipeline Settings: select the RenderPipelineAsset created in 1)
  4) On Camera, using Lightweight Render Pipeline, disable MSAA
  5) Camera->Add Component->Post-Process Layer
  6) Camera->Post-Process Layer->Layer: Everything (as example)
  7) Camera->Add Component->Post-Process Volume
  8) Camera->Post-Process Volume->Is Global: check (as example)
  9) Camera->Post-Process Volume->Profile: New
  10) Camera->Post-Process Volume->Add effect... AmplifyCreations->AmplifyOcclusion
  
Documentation

  Please refer to the following website for an up-to-date online manual:

    http://amplify.pt/unity/amplify-occlusion/manual
  
Feedback

  To file error reports, questions or suggestions, you may use 
  our feedback form online:
	
    http://amplify.pt/contact

  Or contact us directly:

    For general inquiries - info@amplify.pt
    For technical support - support@amplify.pt (customers only)
