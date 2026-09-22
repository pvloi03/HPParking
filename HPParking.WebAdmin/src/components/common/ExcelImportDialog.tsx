import { useState, useRef, type ChangeEvent, type DragEvent } from 'react';
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
import { Badge } from '@/components/ui/badge';
import {
  FileSpreadsheet,
  Upload,
  Download,
  CheckCircle2,
  AlertTriangle,
  XCircle,
  FileText,
  Loader2,
  X,
} from 'lucide-react';
import { toast } from 'sonner';
import { excelApi, extractErrorMessage } from '@/api/excelApi';
import { downloadBlob } from '@/utils/downloadBlob';
import {
  DuplicateMode,
  EXCEL_ENTITY_LABELS,
  type ExcelEntity,
  type ExcelImportResultDto,
} from '@/types/excel';

interface ExcelImportDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  entity: ExcelEntity;
  onSuccess?: () => void;
}

export function ExcelImportDialog({
  open,
  onOpenChange,
  entity,
  onSuccess,
}: ExcelImportDialogProps) {
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [isDragging, setIsDragging] = useState(false);
  const [dryRun, setDryRun] = useState(false);
  const [duplicateMode, setDuplicateMode] = useState<DuplicateMode>(DuplicateMode.Skip);
  const [isLoading, setIsLoading] = useState(false);
  const [isDownloadingTemplate, setIsDownloadingTemplate] = useState(false);
  const [importResult, setImportResult] = useState<ExcelImportResultDto | null>(null);

  const fileInputRef = useRef<HTMLInputElement>(null);

  const entityTitle = EXCEL_ENTITY_LABELS[entity] || entity;

  const handleReset = () => {
    setSelectedFile(null);
    setImportResult(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const handleClose = () => {
    handleReset();
    onOpenChange(false);
  };

  const handleDragOver = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragging(true);
  };

  const handleDragLeave = () => {
    setIsDragging(false);
  };

  const handleDrop = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragging(false);
    const file = e.dataTransfer.files?.[0];
    if (file) {
      validateAndSetFile(file);
    }
  };

  const handleFileChange = (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      validateAndSetFile(file);
    }
  };

  const validateAndSetFile = (file: File) => {
    if (
      !file.name.endsWith('.xlsx') &&
      file.type !==
        'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
    ) {
      toast.error('Vui lòng chọn tệp bảng tính định dạng Excel (.xlsx).');
      return;
    }

    if (file.size > 20 * 1024 * 1024) {
      toast.error('Kích thước tệp vượt quá 20MB.');
      return;
    }

    setSelectedFile(file);
    setImportResult(null);
  };

  const handleDownloadTemplate = async () => {
    try {
      setIsDownloadingTemplate(true);
      const blob = await excelApi.downloadTemplate(entity);
      downloadBlob(blob, `mau_nhap_lieu_${entity}.xlsx`);
      toast.success(`Đã tải tệp mẫu ${entityTitle} thành công`);
    } catch (err) {
      toast.error(extractErrorMessage(err));
    } finally {
      setIsDownloadingTemplate(false);
    }
  };

  const handleImport = async () => {
    if (!selectedFile) {
      toast.error('Vui lòng chọn tệp Excel trước khi thực hiện.');
      return;
    }

    try {
      setIsLoading(true);
      const res = await excelApi.importData(entity, selectedFile, dryRun, duplicateMode);
      setImportResult(res);

      if (res.isDryRun) {
        if (res.failedCount === 0) {
          toast.success(
            `Kiểm tra thử nghiệm hoàn tất: Tất cả ${res.successCount} dòng đều hợp lệ.`
          );
        } else {
          toast.warning(
            `Kiểm tra thử nghiệm: Phát hiện ${res.failedCount} dòng lỗi trên tổng số ${res.totalRows} dòng.`
          );
        }
      } else {
        if (res.failedCount === 0) {
          toast.success(
            `Đã nhập thành công ${res.successCount} bản ghi vào hệ thống!`
          );
          onSuccess?.();
        } else {
          toast.warning(
            `Nhập hoàn tất một phần: ${res.successCount} thành công, ${res.failedCount} thất bại, ${res.skippedCount} bỏ qua.`
          );
          onSuccess?.();
        }
      }
    } catch (err) {
      toast.error(extractErrorMessage(err));
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="sm:max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <div className="flex items-center gap-2 mb-1">
            <div className="p-2 rounded-lg bg-emerald-100 dark:bg-emerald-950 text-emerald-600 dark:text-emerald-400">
              <FileSpreadsheet className="h-5 w-5" />
            </div>
            <DialogTitle className="text-base font-bold">
              Nhập Dữ Liệu Excel: {entityTitle}
            </DialogTitle>
          </div>
          <DialogDescription>
            Tải lên tệp bảng tính .xlsx để nhập hàng loạt hoặc cập nhật danh mục vào hệ thống.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-1">
          {/* NÚT TẢI TỆP MẪU CHUẨN */}
          <div className="flex items-center justify-between p-3 rounded-xl bg-blue-50 dark:bg-blue-950/40 border border-blue-200 dark:border-blue-900 text-xs">
            <div className="space-y-0.5">
              <span className="font-semibold text-blue-900 dark:text-blue-300 block">
                Chưa có tệp dữ liệu mẫu?
              </span>
              <span className="text-muted-foreground block text-[11px]">
                Tải tệp mẫu Excel có định dạng sẵn cột và quy tắc để nhập liệu chính xác.
              </span>
            </div>
            <Button
              variant="outline"
              size="sm"
              onClick={handleDownloadTemplate}
              disabled={isDownloadingTemplate}
              className="h-8 gap-1.5 text-xs text-blue-600 border-blue-200 hover:bg-blue-100 dark:border-blue-800 dark:hover:bg-blue-900/50 cursor-pointer shrink-0"
            >
              {isDownloadingTemplate ? (
                <Loader2 className="h-3.5 w-3.5 animate-spin" />
              ) : (
                <Download className="h-3.5 w-3.5" />
              )}
              <span>Tải tệp mẫu</span>
            </Button>
          </div>

          {/* VÙNG KÉO THẢ TỆP EXCEL */}
          <div
            onDragOver={handleDragOver}
            onDragLeave={handleDragLeave}
            onDrop={handleDrop}
            onClick={() => fileInputRef.current?.click()}
            className={`border-2 border-dashed rounded-xl p-6 text-center cursor-pointer transition-colors ${
              isDragging
                ? 'border-emerald-500 bg-emerald-50 dark:bg-emerald-950/20'
                : selectedFile
                ? 'border-emerald-300 dark:border-emerald-800 bg-card'
                : 'border-border hover:border-emerald-400 dark:hover:border-emerald-700 bg-card/60'
            }`}
          >
            <input
              ref={fileInputRef}
              type="file"
              accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
              onChange={handleFileChange}
              className="hidden"
            />

            {selectedFile ? (
              <div className="flex items-center justify-center gap-3">
                <FileText className="h-8 w-8 text-emerald-600" />
                <div className="text-left">
                  <p className="text-xs font-semibold text-foreground truncate max-w-xs">
                    {selectedFile.name}
                  </p>
                  <p className="text-[11px] text-muted-foreground">
                    {(selectedFile.size / 1024).toFixed(1)} KB • Sẵn sàng tải lên
                  </p>
                </div>
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={(e) => {
                    e.stopPropagation();
                    handleReset();
                  }}
                  className="h-7 w-7 p-0 text-muted-foreground hover:text-destructive cursor-pointer ml-2"
                  title="Chọn tệp khác"
                >
                  <X className="h-4 w-4" />
                </Button>
              </div>
            ) : (
              <div className="space-y-1.5">
                <Upload className="h-8 w-8 text-muted-foreground mx-auto" />
                <p className="text-xs font-semibold text-foreground">
                  Kéo thả tệp .xlsx vào đây hoặc nhấp để chọn tệp
                </p>
                <p className="text-[11px] text-muted-foreground">
                  Hỗ trợ định dạng Excel (.xlsx), kích thước tối đa 20MB
                </p>
              </div>
            )}
          </div>

          {/* CẤU HÌNH NHẬP LIỆU: TRÙNG MÃ & DRY-RUN */}
          <div className="p-3 rounded-xl border border-border bg-muted/20 grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="text-xs font-semibold text-foreground">
                Khi phát hiện bản ghi trùng lặp
              </label>
              <Select
                value={String(duplicateMode)}
                onValueChange={(val) => setDuplicateMode(Number(val) as DuplicateMode)}
              >
                <SelectTrigger className="text-xs bg-background h-9">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={String(DuplicateMode.Skip)} className="text-xs">
                    Bỏ qua bản ghi trùng (Giữ nguyên cũ)
                  </SelectItem>
                  <SelectItem value={String(DuplicateMode.Update)} className="text-xs">
                    Ghi đè / Cập nhật thông tin mới
                  </SelectItem>
                  <SelectItem value={String(DuplicateMode.Error)} className="text-xs">
                    Báo lỗi dòng trùng lặp
                  </SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-1 flex flex-col justify-end">
              <div className="flex items-center gap-2 p-2 rounded-lg bg-background border border-border h-9">
                <input
                  type="checkbox"
                  id="dryRunCheckbox"
                  checked={dryRun}
                  onChange={(e) => setDryRun(e.target.checked)}
                  className="rounded border-border text-blue-600 focus:ring-blue-500 h-4 w-4 cursor-pointer"
                />
                <label
                  htmlFor="dryRunCheckbox"
                  className="text-xs font-medium text-foreground cursor-pointer select-none"
                >
                  Chế độ kiểm tra thử (Dry-run, không lưu CSDL)
                </label>
              </div>
            </div>
          </div>

          {/* KẾT QUẢ IMPORT & BẢNG LỖI CHI TIẾT */}
          {importResult && (
            <div className="space-y-3 pt-2">
              <div className="flex flex-wrap items-center gap-2 p-3 rounded-xl bg-muted/40 border border-border text-xs justify-between">
                <div className="flex items-center gap-3">
                  <div className="flex items-center gap-1">
                    <CheckCircle2 className="h-4 w-4 text-emerald-600" />
                    <span>
                      Hợp lệ:{' '}
                      <strong className="text-emerald-700 dark:text-emerald-400 font-mono">
                        {importResult.successCount}
                      </strong>
                    </span>
                  </div>

                  <div className="flex items-center gap-1">
                    <AlertTriangle className="h-4 w-4 text-amber-600" />
                    <span>
                      Bỏ qua:{' '}
                      <strong className="text-amber-700 dark:text-amber-400 font-mono">
                        {importResult.skippedCount}
                      </strong>
                    </span>
                  </div>

                  <div className="flex items-center gap-1">
                    <XCircle className="h-4 w-4 text-destructive" />
                    <span>
                      Lỗi:{' '}
                      <strong className="text-destructive font-mono">
                        {importResult.failedCount}
                      </strong>
                    </span>
                  </div>
                </div>

                <Badge variant={importResult.isDryRun ? 'secondary' : 'default'} className="text-[10px]">
                  {importResult.isDryRun ? 'Chạy thử nghiệm' : 'Đã nhập vào hệ thống'}
                </Badge>
              </div>

              {/* Bảng danh sách dòng lỗi */}
              {importResult.errors.length > 0 && (
                <div className="rounded-xl border border-destructive/30 bg-destructive/5 overflow-hidden">
                  <div className="p-2.5 bg-destructive/10 border-b border-destructive/20 text-xs font-bold text-destructive flex items-center gap-1.5">
                    <AlertTriangle className="h-3.5 w-3.5" />
                    <span>Chi Tiết Các Dòng Dữ Liệu Không Hợp Lệ ({importResult.errors.length} lỗi)</span>
                  </div>
                  <div className="max-h-48 overflow-y-auto">
                    <table className="w-full text-left text-xs">
                      <thead className="bg-muted/60 text-muted-foreground border-b border-border">
                        <tr>
                          <th className="px-3 py-1.5 w-16">Dòng</th>
                          <th className="px-3 py-1.5 w-28">Cột</th>
                          <th className="px-3 py-1.5 w-32">Giá trị</th>
                          <th className="px-3 py-1.5">Nguyên nhân lỗi</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-border/60">
                        {importResult.errors.map((err, idx) => (
                          <tr key={idx} className="hover:bg-destructive/10">
                            <td className="px-3 py-1.5 font-mono font-bold text-foreground">
                              #{err.row}
                            </td>
                            <td className="px-3 py-1.5 font-medium text-foreground">
                              {err.column}
                            </td>
                            <td className="px-3 py-1.5 font-mono text-muted-foreground truncate max-w-[120px]">
                              {err.value || '—'}
                            </td>
                            <td className="px-3 py-1.5 text-destructive font-medium">
                              {err.errorMessage}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}
            </div>
          )}
        </div>

        <DialogFooter className="pt-3 gap-2 sm:gap-0">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={handleClose}
            disabled={isLoading}
            className="text-xs h-9 cursor-pointer"
          >
            Đóng
          </Button>
          <Button
            type="button"
            size="sm"
            onClick={handleImport}
            disabled={!selectedFile || isLoading}
            className="text-xs h-9 bg-emerald-600 hover:bg-emerald-700 text-white cursor-pointer gap-1.5"
          >
            {isLoading ? (
              <>
                <Loader2 className="h-3.5 w-3.5 animate-spin" />
                <span>Đang xử lý tệp Excel...</span>
              </>
            ) : (
              <>
                <Upload className="h-3.5 w-3.5" />
                <span>{dryRun ? 'Kiểm tra thử nghiệm' : 'Nhập dữ liệu ngay'}</span>
              </>
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
