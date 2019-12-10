﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace VRtist
{
    public enum MessageType
    {
        JoinRoom = 1,
        CreateRoom,
        LeaveRoom,

        Command = 100,
        Transform,
        Delete,
        Mesh,
        Material,
        Camera,
        Light,
        MeshConnection
    }

    public class NetCommand
    {
        public byte[] data;
        public MessageType messageType;
        public int id;

        public NetCommand()
        {
        }
        public NetCommand(byte[] d, MessageType mtype, int mid = 0 )
        {
            data = d;
            messageType = mtype;
            id = mid;
        }
    }
    
    public class NetGeometry
    {

        public static Dictionary<string, Material> materials = new Dictionary<string, Material>();
        public static Material currentMaterial = null;

        public static Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        public static Dictionary<string, Material[]> meshesMaterials = new Dictionary<string, Material[]>();
        public static Dictionary<string, HashSet<MeshFilter>> meshInstances = new Dictionary<string, HashSet<MeshFilter>>();

        public static byte[] StringToBytes(string[] values)
        {
            int size = sizeof(int);
            for (int i = 0; i < values.Length; i++)
            {
                byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(values[i]);
                size += sizeof(int) + utf8.Length;
            }
                

            byte[] bytes = new byte[size];
            Buffer.BlockCopy(BitConverter.GetBytes(values.Length), 0, bytes, 0, sizeof(int));
            int index = sizeof(int);
            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i];
                byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(value);
                Buffer.BlockCopy(BitConverter.GetBytes(utf8.Length), 0, bytes, index, sizeof(int));
                Buffer.BlockCopy(utf8, 0, bytes, index + sizeof(int), value.Length);
                index += sizeof(int) + value.Length;
            }
            return bytes;
        }

        public static byte[] IntToBytes(int[] vectors)
        {
            byte[] bytes = new byte[sizeof(int) * vectors.Length + sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(vectors.Length), 0, bytes, 0, sizeof(int));
            int index = sizeof(int);
            for (int i = 0; i < vectors.Length; i++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(vectors[i]), 0, bytes, index, sizeof(int));
                index += sizeof(int);
            }
            return bytes;
        }

        public static byte[] Vector3ToBytes(Vector3[] vectors)
        {
            byte[] bytes = new byte[3 * sizeof(float) * vectors.Length + sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(vectors.Length), 0, bytes, 0, sizeof(int));
            int index = sizeof(int);
            for (int i = 0; i < vectors.Length; i++)
            {
                Vector3 vector = vectors[i];
                Buffer.BlockCopy(BitConverter.GetBytes(vector.x), 0, bytes, index + 0, sizeof(float));
                Buffer.BlockCopy(BitConverter.GetBytes(vector.y), 0, bytes, index + sizeof(float), sizeof(float));
                Buffer.BlockCopy(BitConverter.GetBytes(vector.z), 0, bytes, index + 2 * sizeof(float), sizeof(float));
                index += 3 * sizeof(float);
            }
            return bytes;
        }

        public static byte[] Vector3ToBytes(Vector3 vector)
        {
            byte[] bytes = new byte[3 * sizeof(float)];

            Buffer.BlockCopy(BitConverter.GetBytes(vector.x), 0, bytes, 0, sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(vector.y), 0, bytes, sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(vector.z), 0, bytes, 2 * sizeof(float), sizeof(float));
            return bytes;
        }

        public static byte[] Vector2ToBytes(Vector2[] vectors)
        {
            byte[] bytes = new byte[2 * sizeof(float) * vectors.Length + sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(vectors.Length), 0, bytes, 0, sizeof(int));
            int index = sizeof(int);
            for (int i = 0; i < vectors.Length; i++)
            {
                Vector2 vector = vectors[i];
                Buffer.BlockCopy(BitConverter.GetBytes(vector.x), 0, bytes, index + 0, sizeof(float));
                Buffer.BlockCopy(BitConverter.GetBytes(vector.y), 0, bytes, index + sizeof(float), sizeof(float));
                index += 2 * sizeof(float);
            }
            return bytes;
        }

        public static byte[] QuaternionToBytes(Quaternion quaternion)
        {
            byte[] bytes = new byte[4 * sizeof(float)];

            Buffer.BlockCopy(BitConverter.GetBytes(quaternion.x), 0, bytes, 0, sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(quaternion.y), 0, bytes, 1 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(quaternion.z), 0, bytes, 2 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(quaternion.w), 0, bytes, 3 * sizeof(float), sizeof(float));
            return bytes;
        }

        public static byte[] ConcatenateBuffers(List<byte[]> buffers)
        {
            int totalLength = 0;
            foreach (byte[] buffer in buffers)
            {
                totalLength += buffer.Length;
            }
            byte[] resultBuffer = new byte[totalLength];
            int index = 0;
            foreach (byte[] buffer in buffers)
            {
                int size = buffer.Length;
                Buffer.BlockCopy(buffer, 0, resultBuffer, index, size);
                index += size;
            }
            return resultBuffer;
        }

        public static void Delete(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            Transform trf = FindPath(root, data, 0, out bufferIndex);
            if (trf == null)
                return;

            MeshFilter[] meshFilters = trf.GetComponentsInChildren<MeshFilter>();
            for(int i = 0; i < meshFilters.Length; i++)
            {
                MeshFilter meshFilter = meshFilters[i];
                GameObject subObject = meshFilters[i].gameObject;

                bool next = false;
                foreach(var meshInstance in meshInstances)
                {
                    string meshInstanceName = meshInstance.Key;
                    HashSet<MeshFilter> meshFilterInstance = meshInstance.Value;
                    foreach(MeshFilter mf in meshFilterInstance)
                    {
                        if(mf == meshFilter)
                        {
                            meshFilterInstance.Remove(meshFilter);
                            if(meshFilterInstance.Count == 0)
                            {
                                meshInstances.Remove(meshInstanceName);
                                meshesMaterials.Remove(meshInstanceName);
                                meshes.Remove(meshInstanceName);
                            }
                            next = true;
                            break;
                        }
                    }
                    if (next)
                        break;
                }
            }

            GameObject.Destroy(trf.gameObject);
        }

        public static Material DefaultMaterial()
        {
            string name = "defaultMaterial";
            if (materials.ContainsKey(name))
                return materials[name];

            Shader hdrplit = Shader.Find("HDRP/Lit");
            Material material = new Material(hdrplit);
            material.name = name;
            material.SetColor("_BaseColor", new Color(0.8f,0.8f,0.8f));
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 0.5f);
            materials[name] = material;

            return material;
        }

        public static void BuildMaterial(byte[] data)
        {
            int nameLength = (int)BitConverter.ToUInt32(data, 0);
            string name = System.Text.Encoding.UTF8.GetString(data, 4, nameLength);
            Material material;
            if (materials.ContainsKey(name))
                material = materials[name];
            else
            {
                Shader hdrplit = Shader.Find("HDRP/Lit");
                material = new Material(hdrplit);
                material.name = name;
                materials[name] = material;
            }

            int currentIndex = 4 + nameLength;

            float[] buffer = new float[3];
            int size = 3 * sizeof(float);

            Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
            currentIndex += size;
            Color baseColor = new Color(buffer[0], buffer[1], buffer[2]);

            float metallic = BitConverter.ToSingle(data, currentIndex);
            currentIndex += sizeof(float);
            float roughness = BitConverter.ToSingle(data, currentIndex);            
            
            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness",1f - roughness);

            currentMaterial = material;
        }

        public static Transform FindPath(Transform root, byte[] data, int startIndex, out int bufferIndex)
        {
            int pathLength = (int)BitConverter.ToUInt32(data, startIndex);
            string path = System.Text.Encoding.UTF8.GetString(data, 4, pathLength);
            bufferIndex = startIndex + pathLength + 4;

            char[] separator = { '/' };
            string[] splitted = path.Split(separator, 1);
            Transform parent = root;
            foreach (string subPath in splitted)
            {
                Transform transform = parent.Find(subPath);
                if (transform == null)
                {
                    return null;
                }
                parent = transform;
            }
            return parent;
        }

        public static string GetString(byte[] data, int startIndex, out int bufferIndex)
        {
            int strLength = (int)BitConverter.ToUInt32(data, startIndex);
            string str = System.Text.Encoding.UTF8.GetString(data, startIndex + 4, strLength);
            bufferIndex = startIndex + strLength + 4;
            return str;
        }

        public static string GetPathName(Transform root, Transform transform)
        {
            string result = transform.name;
            while(transform.parent && transform.parent != root)
            {
                transform = transform.parent;
                result = transform.name + "/" + result;
            }
            return result;
        }

        public static Transform BuildPath(Transform root, byte[] data, int startIndex, bool includeLeaf, out int bufferIndex)
        {
            string path = GetString(data, startIndex, out bufferIndex);

            string[] splitted = path.Split('/');
            Transform parent = root;
            int length = includeLeaf ? splitted.Length : splitted.Length - 1;
            for (int i = 0; i < length; i++)
            {
                string subPath = splitted[i];
                Transform transform = parent.Find(subPath);
                if(transform == null)
                {
                    transform = new GameObject(subPath).transform;                    
                    transform.parent = parent;
                }
                parent = transform;
            }
            return parent;
        }
        public static Transform BuildTransform(Transform root, byte[] data)
        {
            int currentIndex = 0;
            Transform transform = BuildPath(root, data, 0, true, out currentIndex);

            float[] buffer = new float[4];
            int size = 3 * sizeof(float);

            Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
            currentIndex += size;
            transform.localPosition = new Vector3(buffer[0], buffer[1], buffer[2]);

            size = 4 * sizeof(float);
            Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
            currentIndex += size;
            transform.localRotation = new Quaternion(buffer[0], buffer[1], buffer[2], buffer[3]);

            size = 3 * sizeof(float);
            Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
            currentIndex += size;
            transform.localScale = new Vector3(buffer[0], buffer[1], buffer[2]);

            return transform;
        }

        public static NetCommand BuildTransformCommand(Transform root,Transform transform)
        {
            Transform current = transform;
            string path = current.name;
            while(current.parent && current.parent != root)
            {
                current = current.parent;
                path = current.name + "/" + path;
            }

            byte[] nameBuffer = System.Text.Encoding.UTF8.GetBytes(path);
            byte[] nameBufferSize = BitConverter.GetBytes(nameBuffer.Length);

            byte[] positionBuffer = Vector3ToBytes(transform.localPosition);
            byte[] rotationBuffer = QuaternionToBytes(transform.localRotation);
            byte[] scaleBuffer = Vector3ToBytes(transform.localScale);

            List<byte[]> buffers = new List<byte[]>{ nameBufferSize, nameBuffer, positionBuffer, rotationBuffer, scaleBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Transform);
            return command;
        }

        public static NetCommand BuildMeshCommand(MeshInfos meshInfos)
        {
            Mesh mesh = meshInfos.meshFilter.mesh;
            string name = mesh.name;
            byte[] nameBuffer = System.Text.Encoding.UTF8.GetBytes(name);
            byte[] nameBufferSize = BitConverter.GetBytes(nameBuffer.Length);

            byte[] positions = Vector3ToBytes(mesh.vertices);
            byte[] normals = Vector3ToBytes(mesh.normals);
            byte[] uvs = Vector2ToBytes(mesh.uv);

            // temp only one material
            byte[] materialIndices = new byte[sizeof(int) + 2 * sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(2), 0, materialIndices, 0, sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(0), 0, materialIndices, 4, sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(0), 0, materialIndices, 8, sizeof(int));

            byte[] triangles = IntToBytes(mesh.triangles);

            Material material = meshInfos.meshRenderer.material;
            string[] materialNames = new string[1] { material.name };
            byte[] materials = StringToBytes(materialNames);

            List<byte[]> buffers = new List<byte[]> { nameBufferSize, nameBuffer, positions, normals, uvs, materialIndices, triangles, materials };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Mesh);
            return command;
        }

        public static NetCommand BuildMeshConnectionCommand(Transform root, MeshConnectionInfos meshConnectionInfos)
        {
            Transform transform = meshConnectionInfos.meshTransform;
            Mesh mesh = transform.GetComponent<MeshFilter>().mesh;
            string path = GetPathName(root, transform);
            string[] names = new string[2] { path, mesh.name };

            byte[] namesBuffer = StringToBytes(names);
            NetCommand command = new NetCommand(namesBuffer, MessageType.MeshConnection);
            return command;
        }

        public static NetCommand BuildDeleteMeshCommand(Transform root, DeleteMeshInfos deleteMeshInfos)
        {
            Transform transform = deleteMeshInfos.meshTransform;
            Mesh mesh = transform.GetComponent<MeshFilter>().mesh;
            string path = GetPathName(root, transform);

            byte[] encodedPath = System.Text.Encoding.UTF8.GetBytes(path);
            byte[] encodedPathSize = BitConverter.GetBytes(encodedPath.Length);
            List<byte[]> buffers = new List<byte[]> { encodedPath, encodedPath };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Delete);
            return command;
        }

        public static void BuildCamera(Transform root, byte[] data)
        {
            int tmpIndex = 0;
            string name = GetString(data, 0, out tmpIndex);

            int currentIndex = 0;
            Transform transform = BuildPath(root, data, 0, false, out currentIndex);
            if (transform == null)
                return;

            GameObject camGameObject = null;
            Transform camTransform = transform.Find(name);
            if (camTransform == null)
            {
                camGameObject = Utils.CreateInstance(Resources.Load("Prefabs/Camera") as GameObject, transform);
                camGameObject.name = name;
                camGameObject.transform.GetChild(0).Rotate(0f, 180f, 0f);
                camGameObject.transform.GetChild(0).localScale = new Vector3(-1, 1, 1);
            }
            else
            {
                camGameObject = camTransform.gameObject;
            }

            float focal = BitConverter.ToSingle(data, currentIndex);
            float near = BitConverter.ToSingle(data, currentIndex + sizeof(float));
            float far = BitConverter.ToSingle(data, currentIndex + 2 * sizeof(float));
            float aperture = BitConverter.ToSingle(data, currentIndex + 3 * sizeof(float));
            currentIndex += 4 * sizeof(float);

            Camera.GateFitMode gateFit = (Camera.GateFitMode)BitConverter.ToInt32(data, currentIndex);
            if (gateFit == Camera.GateFitMode.None)
                gateFit = Camera.GateFitMode.Horizontal;
            currentIndex += sizeof(Int32);

            float sensorWidth = BitConverter.ToSingle(data, currentIndex);
            currentIndex += sizeof(float);
            float sensorHeight = BitConverter.ToSingle(data, currentIndex);
            currentIndex += sizeof(float);

            Camera cam = camGameObject.GetComponentInChildren<Camera>();

            // Is it necessary ?
            /////////////////
            CameraController cameraController = camGameObject.GetComponent<CameraController>();
            CameraParameters cameraParameters = (CameraParameters)cameraController.GetParameters();
            cameraParameters.focal = focal;
            //cameraParameters.gateFit = gateFit;
            /////////////////

            cam.focalLength = focal;
            cam.gateFit = gateFit;

            cameraParameters.focal = focal;
            cam.focalLength = focal;
            cam.sensorSize = new Vector2(sensorWidth, sensorHeight);
        }

        public static void BuildLight(Transform root, byte[] data)
        {
            int tmpIndex = 0;
            string name = GetString(data, 0, out tmpIndex);

            int currentIndex = 0;
            Transform transform = BuildPath(root, data, 0, false, out currentIndex);
            if (transform == null)
                return;

            LightType lightType = (LightType)BitConverter.ToInt32(data, currentIndex);
            currentIndex += sizeof(Int32);

            GameObject lightGameObject = null;
            Transform lightTransform = transform.Find(name);
            if (lightTransform == null)
            {
                switch (lightType)
                {
                    case LightType.Directional:
                        lightGameObject = Utils.CreateInstance(Resources.Load("Prefabs/Sun") as GameObject, transform);
                        break;
                    case LightType.Point:
                        lightGameObject = Utils.CreateInstance(Resources.Load("Prefabs/Point") as GameObject, transform);
                        break;
                    case LightType.Spot:
                        lightGameObject = Utils.CreateInstance(Resources.Load("Prefabs/Spot") as GameObject, transform);
                        break;
                }
                lightGameObject.transform.GetChild(0).Rotate(0f, 180f, 0f);
                lightGameObject.name = name;
            }
            else
            {
                lightGameObject = lightTransform.gameObject;
            }


            int shadow = BitConverter.ToInt32(data, currentIndex);
            currentIndex += sizeof(Int32);

            float ColorR = BitConverter.ToSingle(data, currentIndex);
            float ColorG = BitConverter.ToSingle(data, currentIndex + sizeof(float));
            float ColorB = BitConverter.ToSingle(data, currentIndex + 2 * sizeof(float));
            Color lightColor = new Color(ColorR, ColorG, ColorB);
            currentIndex += 3 * sizeof(float);

            float power = BitConverter.ToSingle(data, currentIndex);
            currentIndex += sizeof(float);
            float spotSize = BitConverter.ToSingle(data, currentIndex);
            currentIndex += sizeof(float);
            float spotBlend = BitConverter.ToSingle(data, currentIndex);
            currentIndex += sizeof(float);


            LightController lightController = lightGameObject.GetComponent<LightController>();
            LightParameters lightParameters = (LightParameters)lightController.GetParameters();
            lightParameters.color = lightColor;
            switch(lightType)
            {
                case LightType.Point:
                    lightParameters.intensity = power / 10f;
                    break;
                case LightType.Directional:
                    lightParameters.intensity = power * 1.5f;
                    break;
                case LightType.Spot:
                    lightParameters.intensity = power * 0.4f / 3f;
                    break;
            }

            if (lightType == LightType.Spot)
            {
                lightParameters.SetRange(1000f);
                lightParameters.SetOuterAngle(spotSize * 180f / 3.14f);
                lightParameters.SetInnerAngle((1f - spotBlend) * 100f);
            }
            lightParameters.castShadows = shadow != 0 ? true : false;
        }

        public static Transform ConnectMesh(Transform root, byte[] data)
        {
            int currentIndex = 0;
            Transform transform = BuildPath(root, data, 0, true, out currentIndex);
            string meshName = GetString(data, currentIndex, out currentIndex);

            GameObject gobject = transform.gameObject;
            MeshFilter meshFilter = gobject.GetComponent<MeshFilter>();
            if (meshFilter == null)
                meshFilter = gobject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = gobject.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                meshRenderer = gobject.AddComponent<MeshRenderer>();

            Mesh mesh = meshes[meshName];

            meshRenderer.sharedMaterials = meshesMaterials[meshName];

            MeshCollider collider = gobject.AddComponent<MeshCollider>();
            gobject.tag = "PhysicObject";

            if(!meshInstances.ContainsKey(meshName))
                meshInstances[meshName] = new HashSet<MeshFilter>();
            meshInstances[meshName].Add(meshFilter);
            meshFilter.mesh = mesh;
            
            return transform;
        }

        public static Mesh BuildMesh(Transform root, byte[] data)
        {
            int currentIndex = 0;
            string meshName = GetString(data, currentIndex, out currentIndex);

            int verticesCount = (int)BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;
            int size = verticesCount * sizeof(float) * 3;
            Vector3[] vertices = new Vector3[verticesCount];
            float[] float3Values = new float[verticesCount * 3];
            Buffer.BlockCopy(data, currentIndex, float3Values, 0, size);
            int idx = 0;
            for(int i = 0; i < verticesCount; i++)
            {
                vertices[i].x = float3Values[idx++];
                vertices[i].y = float3Values[idx++];
                vertices[i].z = float3Values[idx++];
            }
            currentIndex += size;

            int normalsCount = (int)BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;
            size = normalsCount * sizeof(float) * 3;
            Vector3[] normals = new Vector3[normalsCount];
            Buffer.BlockCopy(data, currentIndex, float3Values, 0, size);
            idx = 0;
            for (int i = 0; i < verticesCount; i++)
            {
                normals[i].x = float3Values[idx++];
                normals[i].y = float3Values[idx++];
                normals[i].z = float3Values[idx++];
            }
            currentIndex += size;

            UInt32 UVsCount = BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;

            size = (int)UVsCount * sizeof(float) * 2;
            Vector2[] uvs = new Vector2[UVsCount];
            Buffer.BlockCopy(data, currentIndex, float3Values, 0, size);
            idx = 0;
            for (int i = 0; i < UVsCount; i++)
            {
                uvs[i].x = float3Values[idx++];
                uvs[i].y = float3Values[idx++];
            }
            currentIndex += size;

            int materialIndicesCount = (int)BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;
            int[] materialIndices = new int[materialIndicesCount * 2];
            size = materialIndicesCount * 2 * sizeof(int);
            Buffer.BlockCopy(data, currentIndex, materialIndices, 0, size);
            currentIndex += size;

            int indicesCount = (int)BitConverter.ToUInt32(data, currentIndex) * 3;
            currentIndex += 4;
            int[] indices = new int[indicesCount];
            size = indicesCount * sizeof(int);
            Buffer.BlockCopy(data, currentIndex, indices, 0, size);
            currentIndex += size;


            int materialCount = (int)BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;
            Material[] meshMaterials;
            if (materialCount == 0)
            {
                meshMaterials = new Material[1];
                meshMaterials[0] = DefaultMaterial();
                materialCount = 1;
            }
            else
            {
                meshMaterials = new Material[materialCount];
                for (int i = 0; i < materialCount; i++)
                {
                    int materialNameSize = (int)BitConverter.ToUInt32(data, currentIndex);
                    string materialName = System.Text.Encoding.UTF8.GetString(data, currentIndex + 4, materialNameSize);
                    currentIndex += materialNameSize + 4;

                    meshMaterials[i] = null;
                    if (materials.ContainsKey(materialName))
                    {
                        meshMaterials[i] = materials[materialName];
                    }
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = meshName;
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            if (materialCount == 1) // only one submesh
                mesh.triangles = indices;
            else
            {
                int remainingTringles = indicesCount / 3;
                int currentTriangleIndex = 0;
                mesh.subMeshCount = materialIndicesCount;

                int[][] subIndices = new int[materialCount][];
                int[] trianglesPerMaterialCount = new int[materialCount];
                int[] subIndicesIndices = new int[materialCount];
                for (int i = 0; i < materialCount; i++)
                {
                    trianglesPerMaterialCount[i] = 0;
                    subIndicesIndices[i] = 0;
                }

                // count
                for (int i = 0; i < materialIndicesCount; i++)
                {
                    int triangleCount = remainingTringles;
                    if (i < (materialIndicesCount - 1))
                    {
                        triangleCount = materialIndices[(i + 1) * 2] - materialIndices[i * 2];
                        remainingTringles -= triangleCount;
                    }
                    int materialIndex = materialIndices[i * 2 + 1];
                    trianglesPerMaterialCount[materialIndex] += triangleCount;
                }

                //allocate
                for(int i = 0; i < materialCount; i++)
                {
                    subIndices[i] = new int[trianglesPerMaterialCount[i] * 3];
                }

                // fill
                remainingTringles = indicesCount / 3;
                for (int i = 0; i < materialIndicesCount; i++)
                {
                    // allocate triangles
                    int triangleCount = remainingTringles;
                    if (i < (materialIndicesCount - 1))
                    {
                        triangleCount = materialIndices[(i + 1) * 2] - materialIndices[i * 2];
                        remainingTringles -= triangleCount;
                    }
                    int materialIndex = materialIndices[i * 2 + 1];
                    int dataSize = triangleCount * 3 * sizeof(int);
                    Buffer.BlockCopy(indices, currentTriangleIndex, subIndices[materialIndex], subIndicesIndices[materialIndex], dataSize);
                    subIndicesIndices[materialIndex] += dataSize;
                    currentTriangleIndex += dataSize;
                }

                // set
                for(int i = 0; i < materialCount; i++)
                {
                    mesh.SetTriangles(subIndices[i], i);
                }
            }

            meshes[meshName] = mesh;
            meshesMaterials[meshName] = meshMaterials;

            if (meshInstances.ContainsKey(meshName))
            {
                HashSet<MeshFilter> filters = meshInstances[meshName];
                foreach (MeshFilter filter in filters)
                {
                    if(filter)
                        filter.mesh = mesh;
                }
            }

            return mesh;
        }
    }

    public class NetworkClient : MonoBehaviour
    {
        private static NetworkClient _instance;
        public Transform root;
        public string host = "localhost";
        public int port = 12800;

        Thread thread = null;
        bool alive = true;

        Socket socket = null;
        List<NetCommand> receivedCommands = new List<NetCommand>();
        List<NetCommand> pendingCommands = new List<NetCommand>();

        public void Awake()
        {
            _instance = this;
        }

        public static NetworkClient GetInstance()
        {
            return _instance;
        }

        void OnDestroy()
        {
            Join();
        }

        void Update()
        {
            lock (this)
            {
                if (receivedCommands.Count == 0)
                    return;

                foreach (NetCommand command in receivedCommands)
                {
                    Debug.Log("Command Id " + command.id.ToString());
                    switch (command.messageType)
                    {
                        case MessageType.Mesh:
                            NetGeometry.BuildMesh(root, command.data);
                            break;
                        case MessageType.MeshConnection:
                            NetGeometry.ConnectMesh(root, command.data);
                            break;
                        case MessageType.Transform:
                            NetGeometry.BuildTransform(root, command.data);
                            break;
                        case MessageType.Material:
                            NetGeometry.BuildMaterial(command.data);
                            break;
                        case MessageType.Camera:
                            NetGeometry.BuildCamera(root, command.data);
                            break;
                        case MessageType.Light:
                            NetGeometry.BuildLight(root, command.data);
                            break;
                        case MessageType.Delete:
                            NetGeometry.Delete(root, command.data);
                            break;
                    }
                }
                receivedCommands.Clear();
            }
        }

        void Start()
        {
            Connect();
        }

        public void Connect()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(host);
            IPAddress ipAddress = ipHostInfo.AddressList[1];
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

            // Create a TCP/IP  socket.  
            socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Connect the socket to the remote endpoint. Catch any errors.  
            try
            {
                socket.Connect(remoteEP);
            }
            catch (ArgumentNullException ane)
            {
                Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
            }
            catch (SocketException se)
            {
                Console.WriteLine("SocketException : {0}", se.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception : {0}", e.ToString());
            }

            JoinRoom("toto");

            thread = new Thread(new ThreadStart(Run));
            thread.Start();
        }
        public void Join()
        {
            if (thread == null)
                return;
            alive = false;
            thread.Join();
            socket.Disconnect(false);
        }

        NetCommand ReadMessage()
        {
            int count = socket.Available;
            if (count < 14)
                return null;

            byte[] header = new byte[14];
            socket.Receive(header, 0, 14, SocketFlags.None);

            var size = BitConverter.ToInt64(header, 0);
            var commandId = BitConverter.ToInt32(header, 8);
            Debug.Log("Received Command Id " + commandId);
            var mtype = BitConverter.ToUInt16(header, 8 + 4);

            byte[] data = new byte[size];
            long remaining = size;
            long current = 0;
            while (remaining > 0)
            {
                int sizeRead = socket.Receive(data, (int)current, (int)remaining, SocketFlags.None);
                current += sizeRead;
                remaining -= sizeRead;
            }


            NetCommand command = new NetCommand(data, (MessageType)mtype);
            return command;
        }

        void WriteMessage(NetCommand command)
        {
            byte[] sizeBuffer = BitConverter.GetBytes((Int64)command.data.Length);
            byte[] commandId = BitConverter.GetBytes((Int32)command.id);
            byte[] typeBuffer = BitConverter.GetBytes((Int16)command.messageType);
            List<byte[]> buffers = new List<byte[]> { sizeBuffer, commandId, typeBuffer, command.data };

            socket.Send(NetGeometry.ConcatenateBuffers(buffers));
        }

        void AddCommand(NetCommand command)
        {
            lock (this)
            {
                pendingCommands.Add(command);
            }
        }

        public void SendTransform(Transform transform)
        {
            NetCommand command = NetGeometry.BuildTransformCommand(root, transform);
            WriteMessage(command);
        }

        public void SendMesh(MeshInfos meshInfos)
        {
            NetCommand command = NetGeometry.BuildMeshCommand(meshInfos);
            WriteMessage(command);
        }

        public void SendMeshConnection(MeshConnectionInfos meshConnectionInfos)
        {
            NetCommand command = NetGeometry.BuildMeshConnectionCommand(root, meshConnectionInfos);
            WriteMessage(command);
        }

        public void SendDeleteMesh(DeleteMeshInfos deleteMeshInfo)
        {
            NetCommand command = NetGeometry.BuildDeleteMeshCommand(root, deleteMeshInfo);
            WriteMessage(command);
        }

        public void JoinRoom(string roomName)
        {
            NetCommand command = new NetCommand(System.Text.Encoding.UTF8.GetBytes(roomName), MessageType.JoinRoom);
            WriteMessage(command);
        }

        void Send(byte[] data)
        {
            lock (this)
            {
                socket.Send(data);
            }
        }

        void Run()
        {
            while(alive)
            {
                NetCommand command = ReadMessage();
                if(command != null)
                {
                    if(command.messageType > MessageType.Command)
                    {
                        lock (this)
                        {
                            receivedCommands.Add(command);
                        }
                    }
                }

                lock (this)
                {
                    if (pendingCommands.Count > 0)
                    {
                        foreach (NetCommand pendingCommand in pendingCommands)
                        {
                            WriteMessage(pendingCommand);
                        }
                        pendingCommands.Clear();
                    }
                }
            }
        }

        public void SendEvent<T>(MessageType messageType, T data)
        {
            switch(messageType)
            {
                case MessageType.Transform:
                    SendTransform(data as Transform); break;
                case MessageType.Mesh:
                    SendMesh(data as MeshInfos); break;
                case MessageType.MeshConnection:
                    SendMeshConnection(data as MeshConnectionInfos); break;
                case MessageType.Delete:
                    SendDeleteMesh(data as DeleteMeshInfos); break;
            }
        }
    }
}