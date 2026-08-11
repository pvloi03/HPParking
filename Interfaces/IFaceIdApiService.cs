using System.Threading.Tasks;

namespace HPParking.Interfaces
{
    public interface IFaceIdApiService
    {
        Task<(bool IsSuccess, string ErrorMessage)> AddUserAsync(string employeeNo, string name, bool isMale);

        Task<(bool IsSuccess, string ErrorMessage)> AddCardAsync(string employeeNo, string cardNumber);

        Task<(bool IsSuccess, string ErrorMessage)> AddFaceImageAsync(string employeeNo, string faceBase64);

        Task<bool> RollbackUserAsync(string employeeNo);
    }
}