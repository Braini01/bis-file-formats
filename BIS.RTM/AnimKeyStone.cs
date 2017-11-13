using BIS.Core.Streams;

namespace BIS.RTM
{
    public enum AnimKeystoneTypeID
    {
        AKSStepSound,
        NAnimKeystoneTypeID,
        AKSUninitialized = -1
    }

    public enum AnimMetaDataID
    {
        AMDWalkCycles,
        AMDAnimLength,
        NAnimMetaDataID,
        AMDUninitialized = -1
    }

    public class AnimKeyStone
    {
        public AnimKeystoneTypeID ID { get; private set; }
        public string StringID { get; private set; }
        public float Time { get; private set; }
        public string Value { get; private set; }

        public AnimKeyStone(BinaryReaderEx input)
        {
            ID = (AnimKeystoneTypeID)input.ReadInt32();
            StringID = input.ReadAsciiz();
            Time = input.ReadSingle();
            Value = input.ReadAsciiz();
        }
    }
}
