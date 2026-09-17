namespace HPParking.SDK.CamPlateSDK
{
    public enum HiChannel : uint
    {
        Channel1 = 1
    }

    public enum HiStream : uint
    {
        Main = 0,
        Sub = 1,
        Third = 2
    }

    public enum HiStreamMode : uint
    {
        TCP = 0
    }

    public enum HiStreamType : byte
    {
        Video = 0x01,
        Audio = 0x02,
        VideoAudio = 0x03
    }
}
