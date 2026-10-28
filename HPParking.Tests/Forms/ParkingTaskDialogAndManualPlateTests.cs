using FluentAssertions;
using HPParking.Core.Models.Entities;
using HPParking.Core.Models.Enums;
using HPParking.Forms;
using System.Windows.Forms;
using Xunit;

namespace HPParking.Tests.Forms
{
    public class ParkingTaskDialogAndManualPlateTests
    {
        [Fact]
        public void ParkingTaskDialog_DismissActive_WhenNoActiveDialog_ShouldNotThrow()
        {
            // Act & Assert
            var act = () =>
            {
                ParkingTaskDialog.DismissActive();
                ParkingTaskDialog.DismissActive();
            };

            act.Should().NotThrow();
        }

        [Fact]
        public void FrmManualPlateInput_DismissActive_WhenNoActiveInstance_ShouldNotThrow()
        {
            // Act & Assert
            var act = () =>
            {
                FrmManualPlateInput.DismissActive();
                FrmManualPlateInput.DismissActive();
            };

            act.Should().NotThrow();
        }

        [Fact]
        public void FrmManualPlateInput_WhenEmptyInput_ShouldDisplayInlineError_WithoutMessageBox()
        {
            RunOnStaThread(() =>
            {
                var lane = new Lane
                {
                    Name = "Làn 1",
                    Direction = LaneDirection.In,
                    InputReader = 1
                };

                using var form = new FrmManualPlateInput(lane, "Camera lỗi", "30E-888.88");
                form.Show();
                form.txtPlate.Text = "   ";

                // Giả lập click nút Xác nhận
                form.btnConfirm.PerformClick();

                // Kiểm tra lỗi hiển thị trực tiếp dạng inline trên form
                form.lblError.Text.Should().Contain("Vui lòng nhập biển số hợp lệ");
                form.EnteredPlate.Should().BeNull();
                form.Close();
            });
        }

        [Fact]
        public void FrmManualPlateInput_WhenPlateMismatch_ShouldDisplayInlineError()
        {
            RunOnStaThread(() =>
            {
                var lane = new Lane
                {
                    Name = "Làn 1",
                    Direction = LaneDirection.In,
                    InputReader = 1
                };

                using var form = new FrmManualPlateInput(lane, "Camera lỗi", expectedPlate: "30E-888.88", mismatchMessage: "Biển số xe không đúng với biển số đăng ký.");
                form.Show();
                form.txtPlate.Text = "29A-999.99";

                // Click Xác nhận
                form.btnConfirm.PerformClick();

                // Lỗi mismatch phải hiển thị dạng inline
                form.lblError.Text.Should().Contain("Biển số xe không đúng với biển số đăng ký");
                form.EnteredPlate.Should().BeNull();
                form.Close();
            });
        }

        [Fact]
        public void FrmManualPlateInput_WhenPlateMatches_ShouldSetEnteredPlateAndCloseWithOk()
        {
            RunOnStaThread(() =>
            {
                var lane = new Lane
                {
                    Name = "Làn 1",
                    Direction = LaneDirection.In,
                    InputReader = 1
                };

                using var form = new FrmManualPlateInput(lane, "Camera lỗi", expectedPlate: "30E-888.88");
                form.Show();
                form.txtPlate.Text = "30E-888.88";

                form.btnConfirm.PerformClick();

                form.EnteredPlate.Should().Be("30E-888.88");
                form.DialogResult.Should().Be(DialogResult.OK);
            });
        }

        [Fact]
        public void FrmManualPlateInput_WhenCancelClicked_ShouldCloseWithCancel()
        {
            RunOnStaThread(() =>
            {
                var lane = new Lane
                {
                    Name = "Làn 1",
                    Direction = LaneDirection.In,
                    InputReader = 1
                };

                using var form = new FrmManualPlateInput(lane, "Camera lỗi", expectedPlate: "30E-888.88");
                form.Show();
                form.txtPlate.Text = "30E-888.88";

                form.btnCancel.PerformClick();

                form.EnteredPlate.Should().BeNull();
                form.DialogResult.Should().Be(DialogResult.Cancel);
            });
        }

        private static void RunOnStaThread(Action action)
        {
            Exception? threadEx = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (threadEx != null)
            {
                throw new Exception("STA Thread Exception", threadEx);
            }
        }
    }
}
