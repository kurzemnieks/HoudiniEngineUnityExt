//#define TILENAME_GROUPS
#define EXPORT_RECT_GRID  //Export minimal bounds (including empty tiles) that encompasses all real tiles 

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HoudiniEngineUnity
{
    using HAPI_NodeId = System.Int32;

    public class HEU_InputInterfaceTilemap : HEU_InputInterface
    {
#if UNITY_EDITOR
        /// <summary>
        /// Registers this input inteface for Unity Tilemap2D on
        /// the callback after scripts are reloaded in Unity.
        /// </summary>
        [InitializeOnLoadMethod]
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            HEU_InputInterfaceTilemap inputInterface = new HEU_InputInterfaceTilemap();
            HEU_InputUtility.RegisterInputInterface(inputInterface);
        }
#endif

        private HEU_InputInterfaceTilemap() : base(priority: DEFAULT_PRIORITY)
        {

        }

        public override bool CreateInputNodeWithDataUpload(HEU_SessionBase session, int connectNodeID, GameObject inputObject, out int inputNodeID)
        {
            inputNodeID = HEU_Defines.HEU_INVALID_NODE_ID;
            if (!HEU_HAPIUtility.IsNodeValidInHoudini(session, connectNodeID))
            {
                Debug.LogError("Connection node is invalid.");
                return false;
            }

            HEU_InputDataTilemap inputTilemap = GenerateTilemapDataFromGameObject(inputObject);

            string inputName = null;
            HAPI_NodeId newNodeID = HEU_Defines.HEU_INVALID_NODE_ID;
            session.CreateInputNode( out newNodeID, inputName );

            if (newNodeID == HEU_Defines.HEU_INVALID_NODE_ID || !HEU_HAPIUtility.IsNodeValidInHoudini(session, newNodeID))
            {
                Debug.LogError("Failed to create new input node in Houdini session!");
                return false;
            }

            inputNodeID = newNodeID;
            if (!session.CookNode(inputNodeID, false))
            {
                Debug.LogError("New input node failed to cook!");
                return false;
            }

            return UploadData(session, inputNodeID, inputTilemap);
        }

        public override bool IsThisInputObjectSupported(GameObject inputObject)
        {
            if (inputObject != null)
            {
                if (inputObject.GetComponentInChildren<Tilemap>(true) != null)
                    return true;
            }
            return false;
        }

        private bool UploadData( HEU_SessionBase session, HAPI_NodeId inputNodeID, HEU_InputData inputData)
        {
            HEU_InputDataTilemap inputTilemap = inputData as HEU_InputDataTilemap;
            if(inputTilemap == null)
            {
                Debug.LogError("Expected HEU_InputDataTilemap type for inputData, but received unssupported type.");
                return false;
            }

            List<Vector3> vertices = new List<Vector3>();            
            List<Vector3> colors = new List<Vector3>();
            //List<Vector3> uvs = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();

            List<string> tileNames = new List<string>();            
            List<Vector3> tileSizes = new List<Vector3>();
            List<Vector3Int> tileCoords = new List<Vector3Int>();

            Tilemap tileMap = inputTilemap._tilemap;
            Grid gridLayout = tileMap.layoutGrid;

            //Get a list of unique tiles used
            TileBase[] usedTiles = new TileBase[ tileMap.GetUsedTilesCount() ];
            tileMap.GetUsedTilesNonAlloc(usedTiles);

            TileBase[] tileArray = tileMap.GetTilesBlock( tileMap.cellBounds );
            //tileArray = tileArray.Where( x => x != null).ToArray(); //only existing tiles

            int tileCount = 0;            
            Vector3 anchorOffset = tileMap.tileAnchor;
            anchorOffset.Scale(gridLayout.cellSize);

            Vector3 pointPos;
            Vector3 pointNormal = new Vector3(0.0f, 0.0f, 1.0f);

            Vector3Int boundsMin = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
            Vector3Int boundsMax = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

            foreach (Vector3Int tilePos in tileMap.cellBounds.allPositionsWithin)
            {
                if (tileMap.HasTile(tilePos))
                {
                    boundsMin = Vector3Int.Min(tilePos, boundsMin);
                    boundsMax = Vector3Int.Max(tilePos, boundsMax);
                }
            }

            boundsMax += Vector3Int.one;
            BoundsInt tileMapBounds = new BoundsInt
            {
                min = boundsMin,
                max = boundsMax
            };

            //foreach (Vector3Int tilePos in tileMap.cellBounds.allPositionsWithin)
            Vector3Int tilePosReverseX = new Vector3Int();
            foreach(Vector3Int tilePos in tileMapBounds.allPositionsWithin)
            {
#if !EXPORT_RECT_GRID
                if(!tileMap.HasTile(tilePos))
                    continue;
#endif

                tilePosReverseX = tilePos;
                //For Hudini (to use Labs Wang Tile tools, we need to reverse point order on the x axis)
                //so we just iterate in reverse order on the x                
                tilePosReverseX.x = tileMapBounds.size.x - 1 - tilePos.x + 2 * tileMapBounds.min.x;

                tileCount++;
                pointPos = tileMap.CellToLocal(tilePosReverseX) + anchorOffset;
                vertices.Add(pointPos);
                normals.Add(pointNormal);

                if (tileMap.HasTile(tilePosReverseX))
                {
                    Tile tile = tileMap.GetTile<Tile>(tilePosReverseX);
                    tileNames.Add(tile.name);
                    colors.Add(new Vector3(tile.color.r, tile.color.g, tile.color.b));
                    tileSizes.Add(new Vector3(tile.sprite.rect.size.x / tile.sprite.pixelsPerUnit, tile.sprite.rect.size.y / tile.sprite.pixelsPerUnit, 0.0f));

                }
                else
                {
                    tileNames.Add("");
                    colors.Add(Vector3.zero);
                    tileSizes.Add(Vector3.zero);
                }

                tileCoords.Add(tilePosReverseX);
            }            

            HAPI_PartInfo partInfo = new HAPI_PartInfo();
            partInfo.faceCount = 0;
            partInfo.vertexCount = 0;
            partInfo.pointCount = tileCount;
            partInfo.pointAttributeCount = 1;
            partInfo.vertexAttributeCount = 0;
            partInfo.primitiveAttributeCount = 0;
            partInfo.detailAttributeCount = 0;

            HAPI_GeoInfo displayGeoInfo = new HAPI_GeoInfo();
            if(!session.GetDisplayGeoInfo(inputNodeID, ref displayGeoInfo))
            {
                return false;
            }

            HAPI_NodeId displayNodeID = displayGeoInfo.nodeId;
            if(!session.SetPartInfo(displayNodeID, 0, ref partInfo))
            {
                Debug.LogError("Failed to set input part info. ");
		        return false;
            }

            if(!HEU_InputMeshUtility.SetMeshPointAttribute(session, displayNodeID, 0, HEU_Defines.HAPI_ATTRIB_POSITION, 3, vertices.ToArray(), ref partInfo, true))
            {
                Debug.LogError("Failed to set point positions.");
                return false;
            }

            if (!HEU_InputMeshUtility.SetMeshPointAttribute(session, displayNodeID, 0, "size", 2, tileSizes.ToArray(), ref partInfo, false))
            {
                Debug.Log("Failed to set tile size attributes. ");
                return false;
            }

            if(!HEU_InputMeshUtility.SetMeshPointAttribute(session, displayNodeID, 0, HEU_Defines.HAPI_ATTRIB_COLOR, 3, colors.ToArray(), ref partInfo, false))
            {
                Debug.Log("Failed to set tile color attributes. ");
                return false;
            }

            if(!HEU_InputMeshUtility.SetMeshPointAttribute(session, displayNodeID, 0, HEU_Defines.HAPI_ATTRIB_NORMAL, 3, normals.ToArray(), ref partInfo, true))
            {
                Debug.Log("Failed to set point normal attributes.");
                return false;
            }

            if(!HEU_InputMeshUtilityExt.SetMeshPointAttribute(session, displayNodeID, 0, "tilepos", 2, tileCoords.ToArray(), ref partInfo))
            {
                Debug.Log("Failed to set point tile coordinates attributes.");
                return false;
            }


#if TILENAME_GROUPS
            //Set point groups based on tile type
            int[] pointGroupMembership = new int[tileCount];
            foreach(TileBase tileType in usedTiles)
            {
                if(!session.AddGroup( displayNodeID, 0, HAPI_GroupType.HAPI_GROUPTYPE_POINT, tileType.name))
                    return false;

                int index = 0;
                foreach( string tileName in tileNames)
                {
                    if(tileName.Equals(tileType.name))
                        pointGroupMembership[index] = 1;
                    else
                        pointGroupMembership[index] = 0;
                    index++;
                }

                if(!session.SetGroupMembership(displayNodeID, 0, HAPI_GroupType.HAPI_GROUPTYPE_POINT, tileType.name, pointGroupMembership, 0, tileCount))
                    return false;
            }
#else
            if(!HEU_InputMeshUtilityExt.SetMeshPointAttribute(session, displayNodeID, 0, "tilename", tileNames.ToArray(), ref partInfo))
            {
                Debug.Log("Failed to set point tile name attributes.");
                return false;
            }
#endif
            if(!HEU_InputMeshUtilityExt.SetMeshDetailAttribute(session, displayNodeID, 0, "bounds", 2, tileMapBounds.size, ref partInfo))
            {
                Debug.Log("Failed to set detail tile map bounds attribute.");
                return false;
            }

            return session.CommitGeo(displayNodeID);
        }

        public class HEU_InputDataTilemap : HEU_InputData
        {
            public Tilemap _tilemap;
            public Transform _transform;
        }

        public HEU_InputDataTilemap GenerateTilemapDataFromGameObject( GameObject inputObject )
        {
            HEU_InputDataTilemap inputTilemap = new HEU_InputDataTilemap();

            Tilemap tileMap = inputObject.GetComponent<Tilemap>();
            if (tileMap != null)
            {
                inputTilemap._tilemap = tileMap;
                inputTilemap._transform = inputObject.transform;
            }

            return inputTilemap;
        }

    }
}