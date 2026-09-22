import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/components/ui/dialog';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Route, Save, Camera, Cpu, ScanFace } from 'lucide-react';
import {
  LaneDirection,
  DeviceType,
  type LaneDto,
  type GateDto,
  type DeviceDto,
  type CreateLaneRequest,
  type UpdateLaneRequest,
} from '@/types/infrastructure';

const laneSchema = z.object({
  gateId: z
    .string()
    .trim()
    .min(1, 'Vui lòng chọn cổng kiểm soát trực thuộc'),
  code: z
    .string()
    .trim()
    .min(2, 'Mã làn xe phải có ít nhất 2 ký tự')
    .max(50, 'Mã làn xe không được quá 50 ký tự')
    .regex(/^[A-Za-z0-9_-]+$/, 'Mã làn chỉ chứa chữ, số, gạch dưới hoặc gạch ngang'),
  name: z
    .string()
    .trim()
    .min(2, 'Tên làn xe phải có ít nhất 2 ký tự')
    .max(100, 'Tên làn xe không được quá 100 ký tự'),
  direction: z.number().int().min(1).max(3),
  plateCameraDeviceId: z.string().optional(),
  overviewCameraDeviceId: z.string().optional(),
  controllerDeviceId: z.string().optional(),
  faceDeviceId: z.string().optional(),
  outputRelay: z.coerce
    .number()
    .int()
    .min(1, 'Rơ-le barrier từ 1 đến 4')
    .max(4, 'Rơ-le barrier từ 1 đến 4'),
  inputReader: z.coerce
    .number()
    .int()
    .min(1, 'Đầu đọc thẻ từ 1 đến 4')
    .max(4, 'Đầu đọc thẻ từ 1 đến 4'),
  isActive: z.boolean(),
});

type LaneFormData = z.infer<typeof laneSchema>;

interface LaneFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  initialData?: LaneDto | null;
  gates: GateDto[];
  devices: DeviceDto[];
  onSubmit: (data: CreateLaneRequest | UpdateLaneRequest) => Promise<void>;
  isSubmitting?: boolean;
}

export function LaneFormDialog({
  open,
  onOpenChange,
  initialData,
  gates,
  devices,
  onSubmit,
  isSubmitting = false,
}: LaneFormDialogProps) {
  const isEditing = Boolean(initialData);

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    watch,
    formState: { errors },
  } = useForm<LaneFormData>({
    resolver: zodResolver(laneSchema),
    defaultValues: {
      gateId: '',
      code: '',
      name: '',
      direction: LaneDirection.In,
      plateCameraDeviceId: 'none',
      overviewCameraDeviceId: 'none',
      controllerDeviceId: 'none',
      faceDeviceId: 'none',
      outputRelay: 1,
      inputReader: 1,
      isActive: true,
    },
  });

  const selectedGateId = watch('gateId');
  const selectedDirection = watch('direction');
  const selectedPlateCamera = watch('plateCameraDeviceId');
  const selectedOverviewCamera = watch('overviewCameraDeviceId');
  const selectedController = watch('controllerDeviceId');
  const selectedFaceDevice = watch('faceDeviceId');
  const isActive = watch('isActive');

  // Lọc danh sách thiết bị theo từng loại
  const cameraDevices = devices.filter((d) => d.type === DeviceType.Camera);
  const controllerDevices = devices.filter((d) => d.type === DeviceType.Controller);
  const faceDevices = devices.filter((d) => d.type === DeviceType.FaceId);

  useEffect(() => {
    if (open) {
      if (initialData) {
        reset({
          gateId: initialData.gateId || '',
          code: initialData.code,
          name: initialData.name,
          direction: initialData.direction,
          plateCameraDeviceId: initialData.plateCameraDeviceId || 'none',
          overviewCameraDeviceId: initialData.overviewCameraDeviceId || 'none',
          controllerDeviceId: initialData.controllerDeviceId || 'none',
          faceDeviceId: initialData.faceDeviceId || 'none',
          outputRelay: initialData.outputRelay || 1,
          inputReader: initialData.inputReader || 1,
          isActive: initialData.isActive,
        });
      } else {
        reset({
          gateId: gates.length > 0 ? gates[0].id : '',
          code: '',
          name: '',
          direction: LaneDirection.In,
          plateCameraDeviceId: 'none',
          overviewCameraDeviceId: 'none',
          controllerDeviceId: 'none',
          faceDeviceId: 'none',
          outputRelay: 1,
          inputReader: 1,
          isActive: true,
        });
      }
    }
  }, [open, initialData, gates, reset]);

  const onFormSubmit = async (formData: LaneFormData) => {
    const payload: CreateLaneRequest | UpdateLaneRequest = {
      gateId: formData.gateId,
      code: formData.code.toUpperCase(),
      name: formData.name,
      direction: formData.direction as LaneDirection,
      plateCameraDeviceId:
        formData.plateCameraDeviceId && formData.plateCameraDeviceId !== 'none'
          ? formData.plateCameraDeviceId
          : undefined,
      overviewCameraDeviceId:
        formData.overviewCameraDeviceId && formData.overviewCameraDeviceId !== 'none'
          ? formData.overviewCameraDeviceId
          : undefined,
      controllerDeviceId:
        formData.controllerDeviceId && formData.controllerDeviceId !== 'none'
          ? formData.controllerDeviceId
          : undefined,
      faceDeviceId:
        formData.faceDeviceId && formData.faceDeviceId !== 'none'
          ? formData.faceDeviceId
          : undefined,
      outputRelay: formData.outputRelay,
      inputReader: formData.inputReader,
      isActive: formData.isActive,
    };
    await onSubmit(payload);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <div className="flex items-center gap-2 mb-1">
            <div className="p-2 rounded-lg bg-blue-100 dark:bg-blue-950 text-blue-600 dark:text-blue-400">
              <Route className="h-5 w-5" />
            </div>
            <DialogTitle className="text-base font-bold">
              {isEditing ? 'Cập Nhật Cấu Hình Làn Xe' : 'Thêm Mới Làn Xe'}
            </DialogTitle>
          </div>
          <DialogDescription>
            {isEditing
              ? 'Chỉnh sửa hướng di chuyển, gán 4 thiết bị ngoại vi và cổng rơ-le barrier.'
              : 'Chọn cổng quản lý, chiều xe và liên kết các thiết bị ngoại vi cho làn xe mới.'}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onFormSubmit)} className="space-y-3.5 py-1">
          {/* Cổng trực thuộc & Chiều xe */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">
                Cổng kiểm soát <span className="text-destructive">*</span>
              </label>
              <Select
                value={selectedGateId}
                onValueChange={(val) => setValue('gateId', val, { shouldValidate: true })}
              >
                <SelectTrigger className="text-xs">
                  <SelectValue placeholder="-- Chọn cổng --" />
                </SelectTrigger>
                <SelectContent>
                  {gates.map((g) => (
                    <SelectItem key={g.id} value={g.id} className="text-xs">
                      {g.name} ({g.code})
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {errors.gateId && (
                <p className="text-[11px] text-destructive">{errors.gateId.message}</p>
              )}
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">
                Hướng di chuyển <span className="text-destructive">*</span>
              </label>
              <Select
                value={String(selectedDirection)}
                onValueChange={(val) =>
                  setValue('direction', Number(val), { shouldValidate: true })
                }
              >
                <SelectTrigger className="text-xs">
                  <SelectValue placeholder="-- Chọn hướng --" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={String(LaneDirection.In)} className="text-xs">
                    Làn Vào (In)
                  </SelectItem>
                  <SelectItem value={String(LaneDirection.Out)} className="text-xs">
                    Làn Ra (Out)
                  </SelectItem>
                  <SelectItem value={String(LaneDirection.Bidirectional)} className="text-xs">
                    Hai Chiều (Bidirectional)
                  </SelectItem>
                </SelectContent>
              </Select>
              {errors.direction && (
                <p className="text-[11px] text-destructive">{errors.direction.message}</p>
              )}
            </div>
          </div>

          {/* Mã làn & Tên làn */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">
                Mã làn xe <span className="text-destructive">*</span>
              </label>
              <Input
                {...register('code')}
                placeholder="VD: LAN_VAO_01"
                className="uppercase text-xs"
                autoFocus={!isEditing}
              />
              {errors.code && (
                <p className="text-[11px] text-destructive">{errors.code.message}</p>
              )}
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">
                Tên làn xe <span className="text-destructive">*</span>
              </label>
              <Input
                {...register('name')}
                placeholder="VD: Làn Xe Máy Vào 1"
                className="text-xs"
              />
              {errors.name && (
                <p className="text-[11px] text-destructive">{errors.name.message}</p>
              )}
            </div>
          </div>

          {/* LIÊN KẾT 4 THIẾT BỊ NGOẠI VI (ADR 0034) */}
          <div className="p-3 rounded-xl border border-border/80 bg-muted/20 space-y-3">
            <span className="text-xs font-bold text-foreground block">
              Liên kết thiết bị ngoại vi (ADR 0034)
            </span>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              {/* Camera Biển Số */}
              <div className="space-y-1">
                <label className="text-xs font-medium text-foreground flex items-center gap-1.5">
                  <Camera className="h-3.5 w-3.5 text-blue-600" />
                  <span>Camera Biển Số (LPR)</span>
                </label>
                <Select
                  value={selectedPlateCamera}
                  onValueChange={(val) => setValue('plateCameraDeviceId', val)}
                >
                  <SelectTrigger className="text-xs bg-background">
                    <SelectValue placeholder="-- Chưa gán camera biển số --" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none" className="text-xs text-muted-foreground">
                      -- Không dùng / Chưa gán --
                    </SelectItem>
                    {cameraDevices.map((d) => (
                      <SelectItem key={d.id} value={d.id} className="text-xs">
                        {d.name} ({d.ipAddress})
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              {/* Camera Toàn Cảnh */}
              <div className="space-y-1">
                <label className="text-xs font-medium text-foreground flex items-center gap-1.5">
                  <Camera className="h-3.5 w-3.5 text-indigo-600" />
                  <span>Camera Toàn Cảnh (Overview)</span>
                </label>
                <Select
                  value={selectedOverviewCamera}
                  onValueChange={(val) => setValue('overviewCameraDeviceId', val)}
                >
                  <SelectTrigger className="text-xs bg-background">
                    <SelectValue placeholder="-- Chưa gán camera toàn cảnh --" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none" className="text-xs text-muted-foreground">
                      -- Không dùng / Chưa gán --
                    </SelectItem>
                    {cameraDevices.map((d) => (
                      <SelectItem key={d.id} value={d.id} className="text-xs">
                        {d.name} ({d.ipAddress})
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              {/* Bộ Điều Khiển Barrier */}
              <div className="space-y-1">
                <label className="text-xs font-medium text-foreground flex items-center gap-1.5">
                  <Cpu className="h-3.5 w-3.5 text-amber-600" />
                  <span>Bộ Điều Khiển Barrier (Controller)</span>
                </label>
                <Select
                  value={selectedController}
                  onValueChange={(val) => setValue('controllerDeviceId', val)}
                >
                  <SelectTrigger className="text-xs bg-background">
                    <SelectValue placeholder="-- Chưa gán controller --" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none" className="text-xs text-muted-foreground">
                      -- Không dùng / Chưa gán --
                    </SelectItem>
                    {controllerDevices.map((d) => (
                      <SelectItem key={d.id} value={d.id} className="text-xs">
                        {d.name} ({d.ipAddress})
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              {/* Thiết Bị Nhận Diện Khuôn Mặt (FaceID) */}
              <div className="space-y-1">
                <label className="text-xs font-medium text-foreground flex items-center gap-1.5">
                  <ScanFace className="h-3.5 w-3.5 text-emerald-600" />
                  <span>Nhận Diện Khuôn Mặt (FaceID)</span>
                </label>
                <Select
                  value={selectedFaceDevice}
                  onValueChange={(val) => setValue('faceDeviceId', val)}
                >
                  <SelectTrigger className="text-xs bg-background">
                    <SelectValue placeholder="-- Chưa gán thiết bị FaceID --" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none" className="text-xs text-muted-foreground">
                      -- Không dùng / Chưa gán --
                    </SelectItem>
                    {faceDevices.map((d) => (
                      <SelectItem key={d.id} value={d.id} className="text-xs">
                        {d.name} ({d.ipAddress})
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>
          </div>

          {/* Cấu hình chân Rơ-le & Đầu đọc thẻ (1-4) */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">
                Chân Rơ-le Barrier (Relay 1 - 4) <span className="text-destructive">*</span>
              </label>
              <Input
                {...register('outputRelay')}
                type="number"
                min={1}
                max={4}
                className="text-xs font-mono"
              />
              <p className="text-[11px] text-muted-foreground">
                Cổng kích mở Barrier trên bộ điều khiển (mặc định: 1).
              </p>
              {errors.outputRelay && (
                <p className="text-[11px] text-destructive">{errors.outputRelay.message}</p>
              )}
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">
                Cổng Đầu Đọc Thẻ (Reader 1 - 4) <span className="text-destructive">*</span>
              </label>
              <Input
                {...register('inputReader')}
                type="number"
                min={1}
                max={4}
                className="text-xs font-mono"
              />
              <p className="text-[11px] text-muted-foreground">
                Cổng nhận mã thẻ Wiegand trên Controller (mặc định: 1).
              </p>
              {errors.inputReader && (
                <p className="text-[11px] text-destructive">{errors.inputReader.message}</p>
              )}
            </div>
          </div>

          {/* Trạng thái hoạt động */}
          <div className="pt-2 border-t border-border flex items-center justify-between">
            <div>
              <span className="text-xs font-semibold text-foreground block">
                Trạng thái hoạt động
              </span>
              <span className="text-[11px] text-muted-foreground block">
                Kích hoạt làn cho phép quét thẻ và mở barrier tự động
              </span>
            </div>
            <button
              type="button"
              onClick={() => setValue('isActive', !isActive, { shouldDirty: true })}
              className={`relative inline-flex h-5 w-9 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none ${
                isActive ? 'bg-blue-600' : 'bg-muted'
              }`}
            >
              <span
                className={`pointer-events-none inline-block h-4 w-4 transform rounded-full bg-white shadow-lg ring-0 transition duration-200 ease-in-out ${
                  isActive ? 'translate-x-4' : 'translate-x-0'
                }`}
              />
            </button>
          </div>

          <DialogFooter className="pt-3 gap-2 sm:gap-0">
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => onOpenChange(false)}
              disabled={isSubmitting}
              className="text-xs h-9 cursor-pointer"
            >
              Hủy bỏ
            </Button>
            <Button
              type="submit"
              size="sm"
              disabled={isSubmitting}
              className="text-xs h-9 bg-blue-600 hover:bg-blue-700 text-white cursor-pointer"
            >
              <Save className="h-3.5 w-3.5 mr-1.5" />
              <span>{isSubmitting ? 'Đang lưu...' : isEditing ? 'Cập nhật' : 'Thêm mới'}</span>
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
