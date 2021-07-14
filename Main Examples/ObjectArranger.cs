#if (UNITY_EDITOR) 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class ObjectArranger : MonoBehaviour {
    /* To keep from updating, the script will disable itself automatically.
     * Set parameters in the inspector and then enable the script to update changes.
     * Instantiated prefabs are set as children of this GameObject.
     * Children are destroyed and re-added when updated, individual changes to the children will be lost.
     */

    public enum Arrangement { CIRCLE, LINE, ARC }

    public GameObject objectToArrange;
    public Arrangement arrangement;

    // Object is the center of the circle
    [System.Serializable]
    public class CircleParameters {
        public int numberOfObjects = 3;
        public float diameter = 4;
    }
    public CircleParameters circleParameters = new CircleParameters();

    // Object is the start of the line
    [System.Serializable]
    public class LineParameters {
        public int numberOfObjects = 3;
        public float length = 5;
        public float spacing; // If you want to use direct spacing instead of even distribution over length, set length to 0
        public bool horizontalInsteadOfVertical;
    }
    public LineParameters lineParameters = new LineParameters();

    // Object is the start of the arc
    [System.Serializable]
    public class ArcParameters {
        public int numberOfObjects = 5;
        public float length = 7;
        public float height = 2;
    }
    public ArcParameters arcParameters = new ArcParameters();

    void Update() {
        Transform[] objectList = GetComponentsInChildren<Transform>();
        if (objectList.Length > 1) {
            for (int i = 1; i < objectList.Length; i++)
                DestroyImmediate(objectList[i].gameObject);
        }

        Vector3 position = Vector3.zero;
        switch (arrangement) {
            case Arrangement.CIRCLE:
                for (int i = 1; i <= circleParameters.numberOfObjects; i++) {
                    position.x = circleParameters.diameter / 2 * Mathf.Cos(2 * Mathf.PI / circleParameters.numberOfObjects * i);
                    position.z = circleParameters.diameter / 2 * Mathf.Sin(2 * Mathf.PI / circleParameters.numberOfObjects * i);
                    CreateObject(position);
                }
                break;
            case Arrangement.LINE:
                for (int i = 0; i < lineParameters.numberOfObjects; i++) {
                    float pos = 0;

                    if (lineParameters.length == 0) pos = lineParameters.spacing * i;
                    else                            pos = lineParameters.length / lineParameters.numberOfObjects * i;

                    if (lineParameters.horizontalInsteadOfVertical) position.z = pos;
                    else                                            position.y = pos;

                    CreateObject(position);
                }
                break;
            case Arrangement.ARC:
                float spacing = arcParameters.length / arcParameters.numberOfObjects;
                float vertexX = (arcParameters.length - spacing) / 2;
                float vertexY = arcParameters.height;
                float length  = vertexY / Mathf.Pow(-vertexX, 2);

                for (int i = 0; i < arcParameters.numberOfObjects; i++) {
                    position.z = spacing * i;
                    position.y = -length * Mathf.Pow(position.z - vertexX, 2) + vertexY;

                    CreateObject(position);
                }
                break;
        }
        string name = arrangement.ToString().ToLower();
        name = arrangement.ToString().Substring(0, 1) + name.Substring(1);
        gameObject.name = name + "Of" + objectToArrange.name + "s";

        enabled = false;
    }

    void CreateObject(Vector3 position) {
        GameObject arrange = PrefabUtility.InstantiatePrefab(objectToArrange, gameObject.transform) as GameObject;
        arrange.transform.localPosition = position;
    }
}
#endif