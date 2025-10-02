using System;
using System.Collections.Generic;
using UnityEngine;

namespace core {


	public class CombinerPro {


		/// <summary>
		/// Combines meshes and implements onto an existing skinned mesh renderer + bones
		/// </summary>
		/// <param name="skinnedMeshRenderer"></param>
		/// <param name="baseBone"></param>
		/// <param name="material"></param>
		/// <param name="bones"></param>
		/// <param name="meshes"></param>
		/// <param name="uvs">Optional UV's to apply to each mesh</param>
		public static Mesh CombineFastORG ( SkinnedMeshRenderer skinnedMeshRenderer, Transform baseBone, Material [ ] material, Transform [ ] bones, Mesh [ ] meshes, Rect [ ] uvs = null ) {
			if (meshes.Length == 0)
				return null;

			CombineInstance [ ] combineInstances = new CombineInstance[ meshes.Length ];

			for ( int i = 0; i < meshes.Length; i ++ ) {
				if (meshes[ i ] == null)
					continue;

				combineInstances[ i ]           = new CombineInstance ();
				combineInstances[ i ].transform = Matrix4x4.TRS (Vector3.zero, Quaternion.Euler (Vector3.zero), new Vector3 (100, 100, 100));
				combineInstances[ i ].mesh      = meshes[ i ];

			}

			//Copy bind poses from first mesh in array
			Matrix4x4 [ ] bindPoses = meshes[ 0 ].bindposes;

			Mesh combined_new_mesh = new Mesh ();
			combined_new_mesh.CombineMeshes (combineInstances, false, false);
			combined_new_mesh.bindposes = bindPoses;

			//Note: Mesh.boneWeights returns a copy of bone weights (this is undocumented)
			BoneWeight [ ] newboneweights = combined_new_mesh.boneWeights;
			Vector2 [ ]    newUvs         = combined_new_mesh.uv;

			//Blendshape dictionary contains list of matching blendshape indices per mesh. 
			//Index is offset by 1, so 0 = no shape exists for that mesh
			combined_new_mesh.boneWeights = newboneweights;
			Dictionary <string, int [ ]> blendshapes = new Dictionary <string, int [ ]> ();

			//Realign boneweights, apply uv's, and map blendshapes 
			int offset = 0;

			for ( int i = 0; i < meshes.Length; i ++ ) {
				for ( int k = 0; k < meshes[ i ].vertexCount; k ++ ) {
					if (i > 0) {
						newboneweights[ offset + k ].boneIndex0 -= bones.Length * i;
						newboneweights[ offset + k ].boneIndex1 -= bones.Length * i;
						newboneweights[ offset + k ].boneIndex2 -= bones.Length * i;
						newboneweights[ offset + k ].boneIndex3 -= bones.Length * i;
					}

					if (uvs != null)
						newUvs[ offset + k ] = new Vector2 (newUvs[ offset + k ].x * uvs[ i ].width + uvs[ i ].x, newUvs[ offset + k ].y * uvs[ i ].height + uvs[ i ].y);
				}

				offset += meshes[ i ].vertexCount;

				for ( int k = 0; k < meshes[ i ].blendShapeCount; k ++ ) {
					string key = meshes[ i ].GetBlendShapeName (k);

					if (!blendshapes.ContainsKey (key))
						blendshapes[ key ] = new int[ meshes.Length ];

					blendshapes[ key ][ i ] = k + 1;
				}
			}

			Vector3 [ ] deltaVertices = null;
			Vector3 [ ] deltaTangents = null;
			Vector3 [ ] deltaNormals  = null;
			// Assign materials per sub-mesh
			Material[] combinedMaterials = new Material[meshes.Length];
			Array.Copy(material, combinedMaterials, material.Length);
			if (blendshapes.Count > 0) {
				deltaVertices = new Vector3[ combined_new_mesh.vertexCount ];
				deltaTangents = new Vector3[ combined_new_mesh.vertexCount ];
				deltaNormals  = new Vector3[ combined_new_mesh.vertexCount ];
			}

			//We assume all blendshapes only have a single frame, aka 0 (empty) to 1 (full). 
			//So we just copy the last frame in each blendshape to a weight of 1 
			foreach ( KeyValuePair <string, int [ ]> shape in blendshapes ) {
				offset = 0;

				for ( int i = 0; i < meshes.Length; i ++ ) {
					int vcount = meshes[ i ].vertexCount;

					//No blendshape for this mesh
					if (shape.Value[ i ] == 0) {
						//TODO: Research whether it's better to create a new array initially, or manually clear them as needed
						Array.Clear (deltaVertices ?? Array.Empty <Vector3> (), offset, vcount);
						Array.Clear (deltaTangents ?? Array.Empty <Vector3> (), offset, vcount);
						Array.Clear (deltaNormals ?? Array.Empty <Vector3> (), offset, vcount);

						offset += vcount;

						continue;
					}

					//Since GetBlendShapeFrameVertices requires matching sizes of arrays (stupid), we gotta create these every time -_-
					Vector3 [ ] tempDeltaVertices = new Vector3[ vcount ];
					Vector3 [ ] tempDeltaTangents = new Vector3[ vcount ];
					Vector3 [ ] tempDeltaNormals  = new Vector3[ vcount ];

					int frame = ( meshes[ i ].GetBlendShapeFrameCount (shape.Value[ i ] - 1) - 1 );

					meshes[ i ].GetBlendShapeFrameVertices (shape.Value[ i ] - 1, frame, tempDeltaVertices, tempDeltaNormals, tempDeltaTangents);

					Array.Copy (tempDeltaVertices, 0, deltaVertices ?? Array.Empty <Vector3> (), offset, vcount);
					Array.Copy (tempDeltaNormals, 0, deltaNormals ?? Array.Empty <Vector3> (), offset, vcount);
					Array.Copy (tempDeltaTangents, 0, deltaTangents ?? Array.Empty <Vector3> (), offset, vcount);

					offset += vcount;
				}

				//Apply
				combined_new_mesh.AddBlendShapeFrame (shape.Key, 1, deltaVertices, deltaNormals, deltaTangents);
			}

			if (uvs != null)
				combined_new_mesh.uv = newUvs;


			combined_new_mesh.RecalculateBounds ();
			combined_new_mesh.RecalculateNormals ();

			skinnedMeshRenderer.sharedMesh      = combined_new_mesh;
			skinnedMeshRenderer.sharedMaterials = combinedMaterials;
			skinnedMeshRenderer.bones           = bones;
			skinnedMeshRenderer.rootBone        = baseBone;

			return combined_new_mesh;
		}

		public static Mesh CombineFastX ( SkinnedMeshRenderer skinnedMeshRenderer, Transform baseBone, Material [ ] materials, Transform [ ] bones, Mesh [ ] meshes, Rect [ ] uvs = null ) {
			if (meshes.Length == 0 || materials.Length == 0)
				return null;

			CombineInstance [ ] combineInstances = new CombineInstance[ meshes.Length ];

			for ( int i = 0; i < meshes.Length; i ++ ) {
				if (meshes[ i ] == null)
					continue;

				combineInstances[ i ]           = new CombineInstance ();
				combineInstances[ i ].transform = Matrix4x4.TRS (Vector3.zero, Quaternion.Euler (Vector3.zero), new Vector3 (100, 100, 100));
				combineInstances[ i ].mesh      = meshes[ i ];
			}

			// Copy bind poses from the first mesh in array
			Matrix4x4 [ ] bindPoses = meshes[ 0 ].bindposes;

			Mesh combined_new_mesh = new Mesh ();
			combined_new_mesh.bindposes = bindPoses;
			combined_new_mesh.CombineMeshes (combineInstances, false, false);

			// Assign materials directly without merging submeshes
			combined_new_mesh.subMeshCount = meshes.Length;

			for ( int i = 0; i < meshes.Length; i ++ ) {
				combined_new_mesh.SetTriangles (meshes[ i ].GetTriangles (0), i); // Assuming only one submesh per mesh
			}

			// Note: Mesh.boneWeights returns a copy of bone weights (this is undocumented)
			BoneWeight [ ] newBoneWeights = combined_new_mesh.boneWeights;
			Vector2 [ ]    newUvs         = combined_new_mesh.uv;

			// Blendshape dictionary contains a list of matching blendshape indices per mesh. 
			// The index is offset by 1, so 0 = no shape exists for that mesh
			Dictionary <string, int [ ]> blendshapes = new Dictionary <string, int [ ]> ();

			// Realign bone weights, apply UVs, and map blendshapes 
			int offset = 0;

			for ( int i = 0; i < meshes.Length; i ++ ) {
				for ( int k = 0; k < meshes[ i ].vertexCount; k ++ ) {
					if (i > 0) {
						newBoneWeights[ offset + k ].boneIndex0 -= bones.Length * i;
						newBoneWeights[ offset + k ].boneIndex1 -= bones.Length * i;
						newBoneWeights[ offset + k ].boneIndex2 -= bones.Length * i;
						newBoneWeights[ offset + k ].boneIndex3 -= bones.Length * i;
					}

					if (uvs != null)
						newUvs[ offset + k ] = new Vector2 (newUvs[ offset + k ].x * uvs[ i ].width + uvs[ i ].x, newUvs[ offset + k ].y * uvs[ i ].height + uvs[ i ].y);
				}

				offset += meshes[ i ].vertexCount;

				for ( int k = 0; k < meshes[ i ].blendShapeCount; k ++ ) {
					string key = meshes[ i ].GetBlendShapeName (k);

					if (!blendshapes.ContainsKey (key))
						blendshapes[ key ] = new int[ meshes.Length ];

					blendshapes[ key ][ i ] = k + 1;
				}
			}

			Vector3 [ ] deltaVertices = null;
			Vector3 [ ] deltaTangents = null;
			Vector3 [ ] deltaNormals  = null;

			if (blendshapes.Count > 0) {
				deltaVertices = new Vector3[ combined_new_mesh.vertexCount ];
				deltaTangents = new Vector3[ combined_new_mesh.vertexCount ];
				deltaNormals  = new Vector3[ combined_new_mesh.vertexCount ];
			}

			// We assume all blendshapes only have a single frame, aka 0 (empty) to 1 (full). 
			// So we just copy the last frame in each blendshape to a weight of 1 
			foreach ( KeyValuePair <string, int [ ]> shape in blendshapes ) {
				offset = 0;

				for ( int i = 0; i < meshes.Length; i ++ ) {
					int vcount = meshes[ i ].vertexCount;

					// No blendshape for this mesh
					if (shape.Value[ i ] == 0) {
						// TODO: Research whether it's better to create a new array initially, or manually clear them as needed
						Array.Clear (deltaVertices ?? Array.Empty <Vector3> (), offset, vcount);
						Array.Clear (deltaTangents ?? Array.Empty <Vector3> (), offset, vcount);
						Array.Clear (deltaNormals ?? Array.Empty <Vector3> (), offset, vcount);

						offset += vcount;

						continue;
					}

					// Since GetBlendShapeFrameVertices requires matching sizes of arrays (stupid), we gotta create these every time -_-
					Vector3 [ ] tempDeltaVertices = new Vector3[ vcount ];
					Vector3 [ ] tempDeltaTangents = new Vector3[ vcount ];
					Vector3 [ ] tempDeltaNormals  = new Vector3[ vcount ];

					int frame = ( meshes[ i ].GetBlendShapeFrameCount (shape.Value[ i ] - 1) - 1 );

					meshes[ i ].GetBlendShapeFrameVertices (shape.Value[ i ] - 1, frame, tempDeltaVertices, tempDeltaNormals, tempDeltaTangents);

					Array.Copy (tempDeltaVertices, 0, deltaVertices ?? Array.Empty <Vector3> (), offset, vcount);
					Array.Copy (tempDeltaNormals, 0, deltaNormals ?? Array.Empty <Vector3> (), offset, vcount);
					Array.Copy (tempDeltaTangents, 0, deltaTangents ?? Array.Empty <Vector3> (), offset, vcount);

					offset += vcount;
				}

				// Apply
				combined_new_mesh.AddBlendShapeFrame (shape.Key, 1, deltaVertices, deltaNormals, deltaTangents);
			}

			if (uvs != null)
				combined_new_mesh.uv = newUvs;

			combined_new_mesh.boneWeights = newBoneWeights;

			combined_new_mesh.RecalculateBounds ();
			combined_new_mesh.RecalculateNormals ();

			skinnedMeshRenderer.sharedMesh      = combined_new_mesh;
			skinnedMeshRenderer.sharedMaterials = materials;
			skinnedMeshRenderer.bones           = bones;
			skinnedMeshRenderer.rootBone        = baseBone;

			return combined_new_mesh;
		}


		
		public static Mesh CombineFast(SkinnedMeshRenderer skinnedMeshRenderer, Transform baseBone, Material[] materials, Transform[] bones, Mesh[] meshes, Rect[] uvs = null)
{
    if (meshes.Length == 0)
        return null;

    CombineInstance[] combineInstances = new CombineInstance[meshes.Length];

    for (int i = 0; i < meshes.Length; i++)
    {
        if (meshes[i] == null)
            continue;

        combineInstances[i] = new CombineInstance();
        combineInstances[i].transform = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(Vector3.zero), new Vector3(100, 100, 100));
        combineInstances[i].mesh = meshes[i];
    }

    // Copy bind poses from the first mesh in array
    Matrix4x4[] bindPoses = meshes[0].bindposes;

    Mesh combinedNewMesh = new Mesh();
    combinedNewMesh.CombineMeshes(combineInstances, false, false);

    // Assign materials per sub-mesh
    Material[] combinedMaterials = new Material[meshes.Length];
    Array.Copy(materials, combinedMaterials, materials.Length);

    combinedNewMesh.subMeshCount = meshes.Length;

    for (int i = 0; i < meshes.Length; i++)
    {
        combinedNewMesh.SetTriangles(meshes[i].GetTriangles(0), i);

        // Assign material to the sub-mesh
        combinedMaterials[i] = materials[i];
    }

    combinedNewMesh.bindposes = bindPoses;

    // Note: Mesh.boneWeights returns a copy of bone weights (this is undocumented)
    BoneWeight[] newBoneWeights = combinedNewMesh.boneWeights;
    Vector2[] newUvs = combinedNewMesh.uv;

    // Blendshape handling
    // ... (remaining blendshape handling code)

    if (uvs != null)
        combinedNewMesh.uv = newUvs;

    combinedNewMesh.boneWeights = newBoneWeights;

    combinedNewMesh.RecalculateBounds();
    combinedNewMesh.RecalculateNormals();

    skinnedMeshRenderer.sharedMesh = combinedNewMesh;
    skinnedMeshRenderer.sharedMaterials = combinedMaterials;
    skinnedMeshRenderer.bones = bones;
    skinnedMeshRenderer.rootBone = baseBone;

    return combinedNewMesh;
}
	}

}
