using System.Collections.Generic;
using UnityEngine;

namespace HoudiniEngineUnity
{
    using HAPI_NodeId = System.Int32;
    using HAPI_PartId = System.Int32;

    public static class HEU_InputMeshUtilityExt
    {
        //Set string point attributes
        public static bool SetMeshPointAttribute(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, string attrName,
        string[] data, ref HAPI_PartInfo partInfo)
        {
            HAPI_AttributeInfo attrInfo = new HAPI_AttributeInfo();
            attrInfo.exists = true;
            attrInfo.owner = HAPI_AttributeOwner.HAPI_ATTROWNER_POINT;
            attrInfo.storage = HAPI_StorageType.HAPI_STORAGETYPE_STRING;
            attrInfo.count = partInfo.pointCount;
            attrInfo.tupleSize = 1;
            attrInfo.originalOwner = HAPI_AttributeOwner.HAPI_ATTROWNER_INVALID;

            if (!session.AddAttribute(geoID, 0, attrName, ref attrInfo))
            {
                Debug.Log("Could not create attribute named: " + attrName);
                return false;
            }
            return HEU_GeneralUtility.SetAttributeArray(geoID, partID, attrName, ref attrInfo, data, session.SetAttributeStringData, partInfo.pointCount);
        }

        //Set int point attributes
        public static bool SetMeshPointAttribute(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, string attrName,
        int tupleSize, Vector3Int[] data, ref HAPI_PartInfo partInfo)
        {
            HAPI_AttributeInfo attrInfo = new HAPI_AttributeInfo();
            attrInfo.exists = true;
            attrInfo.owner = HAPI_AttributeOwner.HAPI_ATTROWNER_POINT;
            attrInfo.storage = HAPI_StorageType.HAPI_STORAGETYPE_INT;
            attrInfo.count = partInfo.pointCount;
            attrInfo.tupleSize = tupleSize;
            attrInfo.originalOwner = HAPI_AttributeOwner.HAPI_ATTROWNER_INVALID;

            int[] attrValues = new int[partInfo.pointCount * tupleSize];

            if (session.AddAttribute(geoID, 0, attrName, ref attrInfo))
            {
                for (int i = 0; i < partInfo.pointCount; ++i)
                {
                    attrValues[i * tupleSize + 0] = data[i][0];

                    for (int j = 1; j < tupleSize; ++j)
                    {
                        attrValues[i * tupleSize + j] = data[i][j];
                    }
                }
            }

            return HEU_GeneralUtility.SetAttributeArray(geoID, partID, attrName, ref attrInfo, attrValues, session.SetAttributeIntData, partInfo.pointCount);
        }

        //Set float detail attribute
        public static bool SetMeshDetailAttribute(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, string attrName,
            int tupleSize, Vector3 data, ref HAPI_PartInfo partInfo)
        {
            HAPI_AttributeInfo attrInfo = new HAPI_AttributeInfo();
            attrInfo.exists = true;
            attrInfo.owner = HAPI_AttributeOwner.HAPI_ATTROWNER_DETAIL;
            attrInfo.storage = HAPI_StorageType.HAPI_STORAGETYPE_FLOAT;
            attrInfo.count = 1;
            attrInfo.tupleSize = tupleSize;
            attrInfo.originalOwner = HAPI_AttributeOwner.HAPI_ATTROWNER_INVALID;

            float[] attrValues = new float[tupleSize];
            if (session.AddAttribute(geoID, 0, attrName, ref attrInfo))
            {
                for (int j = 0; j < tupleSize; ++j)
                {
                    attrValues[j] = data[j];
                }
            }

            return HEU_GeneralUtility.SetAttributeArray(geoID, partID, attrName, ref attrInfo, attrValues, session.SetAttributeFloatData, 1);
        }

        //Set int detail attribute
        public static bool SetMeshDetailAttribute(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, string attrName,
            int tupleSize, Vector3Int data, ref HAPI_PartInfo partInfo)
        {
            HAPI_AttributeInfo attrInfo = new HAPI_AttributeInfo();
            attrInfo.exists = true;
            attrInfo.owner = HAPI_AttributeOwner.HAPI_ATTROWNER_DETAIL;
            attrInfo.storage = HAPI_StorageType.HAPI_STORAGETYPE_INT;
            attrInfo.count = 1;
            attrInfo.tupleSize = tupleSize;
            attrInfo.originalOwner = HAPI_AttributeOwner.HAPI_ATTROWNER_INVALID;

            int[] attrValues = new int[tupleSize];
            if (session.AddAttribute(geoID, 0, attrName, ref attrInfo))
            {
                for (int j = 0; j < tupleSize; ++j)
                {
                    attrValues[j] = data[j];
                }
            }

            return HEU_GeneralUtility.SetAttributeArray(geoID, partID, attrName, ref attrInfo, attrValues, session.SetAttributeIntData, 1);
        }

        //Set string detail attribute
        public static bool SetMeshDetailAttribute(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, string attrName,
            string data, ref HAPI_PartInfo partInfo)
        {
            HAPI_AttributeInfo attrInfo = new HAPI_AttributeInfo();
            attrInfo.exists = true;
            attrInfo.owner = HAPI_AttributeOwner.HAPI_ATTROWNER_DETAIL;
            attrInfo.storage = HAPI_StorageType.HAPI_STORAGETYPE_STRING;
            attrInfo.count = 1;
            attrInfo.tupleSize = 1;
            attrInfo.originalOwner = HAPI_AttributeOwner.HAPI_ATTROWNER_INVALID;

            string[] values = new string[1];
            values[0] = data;

            return HEU_GeneralUtility.SetAttributeArray(geoID, partID, attrName, ref attrInfo, values, session.SetAttributeStringData, 1);
        }
    }
}