using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class ParallaxBackground : MonoBehaviour {

    [Serializable]
    public class BackgroundLayer {
        public GameObject gameObject;
        public float speed;

        [ReadOnly] public GameObject[] objects;
        [ReadOnly] public Vector2 spriteSize;
    }

    [ReorderableList]
    public List<BackgroundLayer> backgroundLayers;

    private Vector2 previousCamPos;

    void Start() {
        // Create copies of each layer and place them on either side of the main one
        previousCamPos = transform.position;

        backgroundLayers.ForEach(layer => {
            layer.spriteSize = layer.gameObject.GetComponent<SpriteRenderer>().bounds.size;
            layer.objects = new GameObject[3];

            // Make clones
            layer.objects[0] = layer.gameObject;
            layer.objects[1] = Instantiate(layer.gameObject, transform);
            layer.objects[2] = Instantiate(layer.gameObject, transform);

            // Position them
            layer.objects[0].transform.position = new Vector3(transform.position.x, layer.gameObject.transform.position.y, layer.gameObject.transform.position.z);
            layer.objects[1].transform.position = layer.objects[0].transform.position + new Vector3(layer.spriteSize.x, 0);
            layer.objects[2].transform.position = layer.objects[0].transform.position - new Vector3(layer.spriteSize.x, 0);
        });
    }

    void Update() {
        // TODO: Move on the y-axis
        // Move all the layers
        Vector2 camDelta = previousCamPos - (Vector2) transform.position;

        backgroundLayers.ForEach(layer => {
            layer.objects[0].transform.position += new Vector3(camDelta.x * layer.speed, 0);
            layer.objects[1].transform.position += new Vector3(camDelta.x * layer.speed, 0);
            layer.objects[2].transform.position += new Vector3(camDelta.x * layer.speed, 0);

            // Check if one of the objects needs to be moved
            foreach (GameObject layerObject in layer.objects) {
                if (Mathf.Abs(layerObject.transform.position.x - transform.position.x) > layer.spriteSize.x * 1.5f) {
                    layerObject.transform.position +=
                        Mathf.Sign(transform.position.x - layerObject.transform.position.x) *
                        new Vector3(layer.spriteSize.x * 3, 0);
                }
            }
        });

        previousCamPos = transform.position;
    }
}
