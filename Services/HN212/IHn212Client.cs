using System;
using System.Threading.Tasks;

namespace HPParking.Services.HN212
{
    public interface IHn212Client : IDisposable
    {
        event Action<ReaderStatusDto>? StatusUpdated;
        event Action<string, string>? CardStatusChanged;
        event Action<CardDataDto>? CardScanned;
        event Action<string>? FaceCaptured;
        event Action<FaceCompareResultDto>? FaceCompared;
        event Action<byte[]>? VideoFrameReceived;
        event Action<string, bool>? ConnectionStateChanged;

        bool IsConnected { get; }
        string ServerUrl { get; }
        ReaderStatusDto? CurrentStatus { get; }
        CardDataDto? CurrentCard { get; }

        Task StartAsync();
        Task StopAsync();
        Task<bool> StartCaptureFaceAsync();
        Task<bool> CancelCaptureFaceAsync();
        Task<FaceCompareResultDto?> CompareFaceAsync();
        Task<bool> ReadCardManualAsync();
        Task<ReaderStatusDto?> GetLatestStatusAsync();
        Task<CardDataDto?> GetLatestCardAsync();
    }
}