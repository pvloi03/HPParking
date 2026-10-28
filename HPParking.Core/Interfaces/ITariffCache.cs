using HPParking.Core.Models.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace HPParking.Core.Interfaces
{
    /// <summary>
    /// Giao diện bộ nhớ đệm tra cứu biểu phí (Tariff) trong bộ nhớ, tối ưu hóa hiệu năng quẹt thẻ và tính phí tại làn
    /// </summary>
    public interface ITariffCache
    {
        /// <summary>
        /// Lấy thông tin biểu phí theo ID từ cache (nếu còn hạn TTL) hoặc nạp từ CSDL và lưu cache
        /// </summary>
        Task<Tariff?> GetCachedTariffAsync(string? tariffId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Xóa một biểu phí cụ thể khỏi bộ nhớ đệm
        /// </summary>
        void Invalidate(string tariffId);

        /// <summary>
        /// Xóa sạch toàn bộ bộ nhớ đệm biểu phí
        /// </summary>
        void Clear();
    }
}
