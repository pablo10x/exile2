using Sirenix.OdinInspector;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

struct simplejob : IJob
{
    public void Execute()
    {
        for (int i = 0; i < 100000; i++)
        {
            var ix = math.exp(5000f);
        }
    }
}

public class CharacterCombiner : MonoBehaviour
{
    [Button("Combine")]
    void Combine()
    {
        // Assuming you have your body parts as separate game objects
        SkinnedMeshRenderer[] bodyPartRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();

        CombineInstance[] combineInstances = new CombineInstance[bodyPartRenderers.Length];

        for (int i = 0; i < bodyPartRenderers.Length; i++)
        {
            combineInstances[i].mesh = bodyPartRenderers[i].sharedMesh;
            combineInstances[i].transform = bodyPartRenderers[i].transform.localToWorldMatrix;
        }

        // Create a new game object to hold the combined mesh
        GameObject combinedObject = new GameObject("CombinedBody");

        // Add a SkinnedMeshRenderer to the combined object
        SkinnedMeshRenderer combinedRenderer = combinedObject.AddComponent<SkinnedMeshRenderer>();

        // Combine meshes
        combinedRenderer.sharedMesh = new Mesh();
        combinedRenderer.sharedMesh.CombineMeshes(combineInstances, true, false);

        // Set the combined material (you might need to adjust this based on your setup)
        combinedRenderer.material = bodyPartRenderers[0].material;

        // Optionally, you can destroy the original body part game objects
        foreach (var bodyPartRenderer in bodyPartRenderers)
        {
            Destroy(bodyPartRenderer.gameObject);
        }
    }
}