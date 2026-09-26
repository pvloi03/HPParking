using System;
using System.Windows.Forms;

namespace HPParking.Helper
{
    /// <summary>
    /// Chuyển đổi con trỏ chuột sang trạng thái chờ/tải (WaitCursor) và tự động khôi phục về mặc định khi hoàn thành tác vụ CRUD.
    /// </summary>
    public sealed class WaitCursorScope : IDisposable
    {
        private readonly Control _control;
        private bool _disposed;

        public WaitCursorScope(Control control)
        {
            _control = control ?? throw new ArgumentNullException(nameof(control));
            try
            {
                if (!_control.IsDisposed)
                {
                    _control.UseWaitCursor = true;
                }
                Cursor.Current = Cursors.WaitCursor;
            }
            catch
            {
                // Bỏ qua nếu form/control đang trong tiến trình hủy
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                if (!_control.IsDisposed)
                {
                    _control.UseWaitCursor = false;
                }
                Cursor.Current = Cursors.Default;
            }
            catch
            {
                // Bỏ qua nếu form/control đã bị hủy
            }
        }
    }
}
