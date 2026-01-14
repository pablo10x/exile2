warning: in the working copy of 'Assets/Scenes/Game.unity', LF will be replaced by CRLF the next time Git touches it
warning: in the working copy of 'Assets/Scenes/Initializier.unity', LF will be replaced by CRLF the next time Git touches it
warning: in the working copy of 'Assets/Scenes/Main.unity', LF will be replaced by CRLF the next time Git touches it
warning: in the working copy of 'Assets/Settings/URP-Performant-Renderer.asset', LF will be replaced by CRLF the next time Git touches it
warning: in the working copy of 'Assets/Settings/URP-Performant.asset', LF will be replaced by CRLF the next time Git touches it
[1mdiff --git a/Assets/Scenes/Game.unity b/Assets/Scenes/Game.unity[m
[1mindex b7d2b4a..fcc4676 100644[m
[1m--- a/Assets/Scenes/Game.unity[m
[1m+++ b/Assets/Scenes/Game.unity[m
[36m@@ -31696,6 +31696,50 @@[m [mTransform:[m
     type: 3}[m
   m_PrefabInstance: {fileID: 1960186389}[m
   m_PrefabAsset: {fileID: 0}[m
[32m+[m[32m--- !u!1 &871259182[m
[32m+[m[32mGameObject:[m
[32m+[m[32m  m_ObjectHideFlags: 0[m
[32m+[m[32m  m_CorrespondingSourceObject: {fileID: 0}[m
[32m+[m[32m  m_PrefabInstance: {fileID: 0}[m
[32m+[m[32m  m_PrefabAsset: {fileID: 0}[m
[32m+[m[32m  serializedVersion: 6[m
[32m+[m[32m  m_Component:[m
[32m+[m[32m  - component: {fileID: 871259184}[m
[32m+[m[32m  - component: {fileID: 871259183}[m
[32m+[m[32m  m_Layer: 0[m
[32m+[m[32m  m_Name: FPS[m
[32m+[m[32m  m_TagString: Untagged[m
[32m+[m[32m  m_Icon: {fileID: 0}[m
[32m+[m[32m  m_NavMeshLayer: 0[m
[32m+[m[32m  m_StaticEditorFlags: 0[m
[32m+[m[32m  m_IsActive: 1[m
[32m+[m[32m--- !u!114 &871259183[m
[32m+[m[32mMonoBehaviour:[m
[32m+[m[32m  m_ObjectHideFlags: 0[m
[32m+[m[32m  m_CorrespondingSourceObject: {fileID: 0}[m
[32m+[m[32m  m_PrefabInstance: {fileID: 0}[m
[32m+[m[32m  m_PrefabAsset: {fileID: 0}[m
[32m+[m[32m  m_GameObject: {fileID: 871259182}[m
[32m+[m[32m  m_Enabled: 1[m
[32m+[m[32m  m_EditorHideFlags: 0[m
[32m+[m[32m  m_Script: {fileID: 11500000, guid: ae19a0ea6c8579c49af53202ca16edd5, type: 3}[m
[32m+[m[32m  m_Name:[m[41m [m
[32m+[m[32m  m_EditorClassIdentifier:[m[41m [m
[32m+[m[32m--- !u!4 &871259184[m
[32m+[m[32mTransform:[m
[32m+[m[32m  m_ObjectHideFlags: 0[m
[32m+[m[32m  m_CorrespondingSourceObject: {fileID: 0}[m
[32m+[m[32m  m_PrefabInstance: {fileID: 0}[m
[32m+[m[32m  m_PrefabAsset: {fileID: 0}[m
[32m+[m[32m  m_GameObject: {fileID: 871259182}[m
[32m+[m[32m  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}[m
[32m+[m[32m  m_LocalPosition: {x: 0.070902586, y: 1.2425914, z: -0.09692359}[m
[32m+[m[32m  m_LocalScale: {x: 1, y: 1, z: 1}[m
[32m+[m[32m  m_ConstrainProportionsScale: 0[m
[32m+[m[32m  m_Children: [][m
[32m+[m[32m  m_Father: {fileID: 0}[m
[32m+[m[32m  m_RootOrder: 6[m
[32m+[m[32m  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}[m
 --- !u!1 &930842490[m
 GameObject:[m
   m_ObjectHideFlags: 0[m
[36m@@ -32501,6 +32545,26 @@[m [mPrefabInstance:[m
       propertyPath: orbitCamera[m
       value: [m
       objectReference: {fileID: 1960186390}[m
[32m+[m[32m    - target: {fileID: 4609995985063517220, guid: f84423efd39d93b4ea57192c9383b574,[m
[32m+[m[32m        type: 3}[m
[32m+[m[32m      propertyPath: MaxStableMoveSpeed[m
[32m+[m[32m      value: 0[m
[32m+[m[32m      objectReference: {fileID: 0}[m
[32m+[m[32m    - target: {fileID: 4609995985063517220, guid: f84423efd39d93b4ea57192c9383b574,[m
[32m+[m[32m        type: 3}[m
[32m+[m[32m      propertyPath: orientationSharpness[m
[32m+[m[32m      value: 5[m
[32m+[m[32m      objectReference: {fileID: 0}[m
[32m+[m[32m    - target: {fileID: 4609995985063517220, guid: f84423efd39d93b4ea57192c9383b574,[m
[32m+[m[32m        type: 3}[m
[32m+[m[32m      propertyPath: StableMovementSharpness[m
[32m+[m[32m      value: 10000[m
[32m+[m[32m      objectReference: {fileID: 0}[m
[32m+[m[32m    - target: {fileID: 4609995985063517220, guid: f84423efd39d93b4ea57192c9383b574,[m
[32m+[m[32m        type: 3}[m
[32m+[m[32m      propertyPath: runningThresholdMaxTimer[m
[32m+[m[32m      value: 1.5[m
[32m+[m[32m      objectReference: {fileID: 0}[m
     - target: {fileID: 5258809703288912487, guid: f84423efd39d93b4ea57192c9383b574,[m
         type: 3}[m
       propertyPath: m_IsActive[m
[36m@@ -32576,6 +32640,11 @@[m [mPrefabInstance:[m
       propertyPath: m_IsActive[m
       value: 0[m
       objectReference: {fileID: 0}[m
[32m+[m[32m    - target: {fileID: 6980969227149905021, guid: f84423efd39d93b4ea57192c9383b574,[m
[32m+[m[32m        type: 3}[m
[32m+[m[32m      propertyPath: field of view[m
[32m+[m[32m      value: 60[m
[32m+[m[32m      objectReference: {fileID: 0}[m
     - target: {fileID: 7291162879272245904, guid: f84423efd39d93b4ea57192c9383b574,[m
         type: 3}[m
       propertyPath: m_IsActive[m
[36m@@ -32605,26 +32674,6 @@[m [mMonoBehaviour:[m
   m_Script: {fileID: 11500000, guid: 025107b2e9d6f1c44bf7200ce0f6b5a9, type: 3}[m
   m_Name: [m
   m_EditorClassIdentifier: [m
[31m---- !u!1 &1961823525 stripped[m
[31m-GameObject:[m
[31m-  m_CorrespondingSourceObject: {fileID: 2267273328451895762, guid: d6b3b9c8658e0f248b6f602abb992b2b,[m
[31m-    type: 3}[m
[31m-  m_PrefabInstance: {fileID: 6954368855905843613}[m
[31m-  m_PrefabAsset: {fileID: 0}[m
[31m---- !u!64 &1961823528[m
[31m-MeshCollider:[m
[31m-  m_ObjectHideFlags: 0[m
[31m-  m_CorrespondingSourceObject: {fileID: 0}[m
[31m-  m_PrefabInstance: {fileID: 0}[m
[31m-  m_PrefabAsset: {fileID: 0}[m
[31m-  m_GameObject: {fileID: 1961823525}[m
[31m-  m_Material: {fileID: 0}[m
[31m-  m_IsTrigger: 0[m
[31m-  m_Enabled: 1[m
[31m-  serializedVersion: 4[m
[31m-  m_Convex: 0[m
[31m-  m_CookingOptions: 30[m
[31m-  m_Mesh: {fileID: -8545447256379345787, guid: 46cf6e3b02161c647a77eaf4fc5e7f62, type: 3}[m
 --- !u!4 &1973403501 stripped[m
 Transform:[m
   m_CorrespondingSourceObject: {fileID: 2673296164697833026, guid: f84423efd39d93b4ea57192c9383b574,[m
[36m@@ -32668,6 +32717,16 @@[m [mPrefabInstance:[m
   m_Modification:[m
     m_TransformParent: {fileID: 0}[m
     m_Modifications:[m
[32m+[m[32m    - target: {fileID: 2106109143322732189, guid: d6b3b9c8658e0f248b6f602abb992b2b,[m
[32m+[m[32m        type: 3}[m
[32m+[m[32m      propertyPath: m_IsActive[m
[32m+[m[32m      value: 0[m
[32m+[m[32m      objectReference: {fileID: 0}[m
[32m+[m[32m    - target: {fileID: 2267273326877471053, guid: d6b3b9c8658e0f248b6f602abb992b2b,[m
[32m+[m[32m        type: 3}[m
[32m+[m[32m      propertyPath: m_IsActive[m
[32m+[m[32m      value: 0[m
[32m+[m[32m      objectReference: {fileID: 0}[m
     - target: {fileID: 2267273328272397799, guid: d6b3b9c8658e0f248b6f602abb992b2b,[m
         type: 3}[m
       propertyPath: m_RootOrder[m
[36m@@ -32676,7 +32735,7 @@[m [mPrefabInstance:[m
     - target: {fileID: 2267273328272397799, guid: d6b3b9c8658e0f248b6f602abb992b2b,[m
         type: 3}[m
       propertyPath: m_LocalPosition.x[m
[31m-      value: 2.51[m
[32m+[m[32m      value: 3.1342688[m
       objectReference: {fileID: 0}[m
     - target: {fileID: 2267273328272397799, guid: d6b3b9c8658e0f248b6f602abb992b2b,[m
         type: 3}[m
[36m@@ -32686,12 +32745,12 @@[m [mPrefabInstance:[m
     - target: {fileID: 2267273328272397799, guid: d6b3b9c8658e0f248b6f602abb992b2b,[m
         type: 3}[m
       propertyPath: m_LocalPosition.z[m
[31m-      value: -8.79[m
[32m+[m[32m      value: 12.36861[m
       objectReference: {fileID: 0}[m
     - target: {fileID: 2267273328272397799, guid: d6b3b9c8658e0f248b6f602abb992b2b,[m
         type: 3}[m
       propertyPath: m_LocalRotation.w[m
[31m-      value: 0.70710784[m
[32m+[m[32m      value: 0.866026[m
       objectReference: {fileID: 0}[m
     - target: {fileID: 2267273328272397799, guid: d6b3b9c8658e0f248b6f602abb992b2b,[m
         type: 3}[m
[36m@@ -32701,7 +32760,7 @@[m [mPrefabInstance:[m
     - target: {fileID: 2267273328272397799, guid