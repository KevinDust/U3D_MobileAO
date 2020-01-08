using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    // Start is called before the first frame update
    private void Start()
    {
        Debug.Log(Mathf.Pow(2, 0.5f));
    }

    // Update is called once per frame
    void Update()
    {
        int times = 10000;
        float success = 0;
        Random.InitState(10000);
        List<float> dlist = new List<float>();
        for (int n = 0; n < times; n++) {
            dlist.Clear();
            for (int i = 0; i < 4; i++)
            {
                float degree = Random.Range(0, 360);
                dlist.Add(degree);
            }
            dlist.Sort();
            float totalD = 0;
            for (int f = 0; f < 3; f++)
            {
                float delta = Mathf.Abs(dlist[f + 1] - dlist[f]);
                if (delta > 180)
                {
                    delta = 360 - delta;
                }
                totalD += delta;
            }
            if (totalD <= 180)
            {
                success++;
            }
        }
        Debug.Log(success / times);

    }
}
//Vector2 duck = Random.insideUnitCircle;
//float degree = Vector2.Angle(Vector2.right, duck);
//if (duck.y < 0)
//{
//    degree = 360 - degree;
//}