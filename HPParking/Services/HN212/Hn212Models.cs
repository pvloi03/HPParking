using System;

namespace HPParking.Services.HN212
{
    public class ReaderStatusDto
    {
        public bool IsReaderConnected { get; set; }
        public string ReaderSerialNumber { get; set; } = string.Empty;
        public string CardStatus { get; set; } = "None"; // None, Present, Reading, ReadSuccess, Error
        public bool IsCameraActive { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        public bool IsCardPresent => CardStatus == "Present" || CardStatus == "Reading" || CardStatus == "ReadSuccess";
    }

    public class CardDataDto
    {
        public string DocumentNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string DateOfBirth { get; set; } = string.Empty;
        public string Sex { get; set; } = string.Empty;
        public string Nationality { get; set; } = string.Empty;
        public string Ethnicity { get; set; } = string.Empty;
        public string Religion { get; set; } = string.Empty;
        public string Hometown { get; set; } = string.Empty;
        public string PermanentAddress { get; set; } = string.Empty;

        public string Address => PermanentAddress;

        public string IssueDate { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public string PreviousNumber { get; set; } = string.Empty;
        public string PersonalIdentification { get; set; } = string.Empty;
        public string FatherName { get; set; } = string.Empty;
        public string MotherName { get; set; } = string.Empty;
        public string SpouseName { get; set; } = string.Empty;
        public string Mrz { get; set; } = string.Empty;

        public string ChipFaceBase64 { get; set; } = string.Empty;
        public byte[] ChipFaceBytes => string.IsNullOrEmpty(ChipFaceBase64) ? Array.Empty<byte>() : Convert.FromBase64String(ChipFaceBase64);

        public DateTime ReadTime { get; set; } = DateTime.Now;
    }

    public class FaceCompareResultDto
    {
        public int Score { get; set; }
        public bool IsMatch { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? CapturedFaceBase64 { get; set; }

        public byte[] CapturedFaceBytes => string.IsNullOrEmpty(CapturedFaceBase64) ? Array.Empty<byte>() : Convert.FromBase64String(CapturedFaceBase64);

        public DateTime ComparedTime { get; set; } = DateTime.Now;
    }
}