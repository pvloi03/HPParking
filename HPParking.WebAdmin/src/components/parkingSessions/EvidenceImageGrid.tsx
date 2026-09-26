import { useState, useEffect, useCallback } from 'react';
import {
  ChevronLeft,
  ChevronRight,
  ImageOff,
} from 'lucide-react';

export interface EvidenceImageGridProps {
  inPlateImagePath?: string | null;
  inOverviewImagePath?: string | null;
  outPlateImagePath?: string | null;
  outOverviewImagePath?: string | null;
  plateNumber?: string;
  inLaneName?: string;
  outLaneName?: string;
  inTime?: string;
  outTime?: string;
  isActiveSession?: boolean;
}

export interface SlideItem {
  id: string;
  label: string;
  subtitle: string;
  tag: string;
  tagColor: string;
  url: string;
  hasImg: boolean;
  fallbackText: string;
}

export const formatImageUrl = (input?: string | null): string => {
  if (!input) return '';
  if (input.startsWith('http://') || input.startsWith('https://')) return input;

  const normalized = input.replace(/\\/g, '/');
  return normalized.startsWith('/') ? normalized : `/${normalized}`;
};

export const hasImagePath = (input?: string | null): boolean => {
  if (!input) return false;
  return input.trim().length > 0;
};

export function EvidenceImageGrid({
  inPlateImagePath,
  inOverviewImagePath,
  outPlateImagePath,
  outOverviewImagePath,
  plateNumber = '---',
  inLaneName,
  outLaneName,
  inTime,
  outTime,
  isActiveSession = false,
}: EvidenceImageGridProps) {
  const [activeSlide, setActiveSlide] = useState(0);
  const [failedImages, setFailedImages] = useState<Record<string, boolean>>({});

  const slides: SlideItem[] = [
    {
      id: 'in-overview',
      label: '1. Toàn Cảnh Lúc Vào',
      subtitle: `Làn: ${inLaneName || 'Làn Vào 1'} • Thời gian: ${inTime ? new Date(inTime).toLocaleString('vi-VN') : '--'}`,
      tag: 'VÀO',
      tagColor: 'bg-blue-600',
      url: formatImageUrl(inOverviewImagePath),
      hasImg: hasImagePath(inOverviewImagePath),
      fallbackText: 'Không có ảnh toàn cảnh vào',
    },
    {
      id: 'in-plate',
      label: '2. Cận Cảnh Biển Số Vào',
      subtitle: `Biển số nhận diện: ${plateNumber} • Thời gian: ${inTime ? new Date(inTime).toLocaleString('vi-VN') : '--'}`,
      tag: 'BIỂN SỐ VÀO',
      tagColor: 'bg-cyan-600',
      url: formatImageUrl(inPlateImagePath),
      hasImg: hasImagePath(inPlateImagePath),
      fallbackText: 'Không có ảnh biển số vào',
    },
    {
      id: 'out-overview',
      label: '3. Toàn Cảnh Lúc Ra',
      subtitle: `Làn: ${outLaneName || 'Làn Ra 1'} • Thời gian: ${outTime ? new Date(outTime).toLocaleString('vi-VN') : '--'}`,
      tag: 'RA',
      tagColor: 'bg-emerald-600',
      url: formatImageUrl(outOverviewImagePath),
      hasImg: hasImagePath(outOverviewImagePath),
      fallbackText: isActiveSession
        ? 'Xe đang đỗ trong bãi (Chưa có ảnh toàn cảnh ra)'
        : 'Chưa có ảnh toàn cảnh ra',
    },
    {
      id: 'out-plate',
      label: '4. Cận Cảnh Biển Số Ra',
      subtitle: `Biển số đối chiếu: ${plateNumber} • Thời gian: ${outTime ? new Date(outTime).toLocaleString('vi-VN') : '--'}`,
      tag: 'BIỂN SỐ RA',
      tagColor: 'bg-rose-600',
      url: formatImageUrl(outPlateImagePath),
      hasImg: hasImagePath(outPlateImagePath),
      fallbackText: isActiveSession
        ? 'Xe đang đỗ trong bãi (Chưa có ảnh biển số ra)'
        : 'Chưa có ảnh biển số ra',
    },
  ];

  const handlePrevSlide = useCallback(() => {
    setActiveSlide((prev) => (prev > 0 ? prev - 1 : slides.length - 1));
  }, [slides.length]);

  const handleNextSlide = useCallback(() => {
    setActiveSlide((prev) => (prev < slides.length - 1 ? prev + 1 : 0));
  }, [slides.length]);

  // Hỗ trợ phím mũi tên Trái / Phải để duyệt slide
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'ArrowLeft') handlePrevSlide();
      if (e.key === 'ArrowRight') handleNextSlide();
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [handlePrevSlide, handleNextSlide]);

  const current = slides[activeSlide] ?? slides[0];
  const isCurrentValid = current.hasImg && !failedImages[current.id];

  return (
    <div className="space-y-3">
      {/* Main Hero Slide Stage - Compact 280px */}
      <div className="relative w-full h-[240px] sm:h-[280px] bg-slate-50 dark:bg-slate-950 rounded-xl overflow-hidden flex items-center justify-center border border-slate-200 dark:border-slate-800 group shadow-inner">
        {isCurrentValid ? (
          <img
            key={current.id}
            src={current.url}
            alt={current.label}
            className="w-full h-full object-contain transition-all duration-300 animate-in fade-in zoom-in-95"
            onError={() => {
              setFailedImages((prev) => ({ ...prev, [current.id]: true }));
            }}
          />
        ) : (
          <div className="flex flex-col items-center justify-center text-slate-400 dark:text-slate-500 gap-2.5 p-6 text-center select-none animate-in fade-in">
            <div className="h-12 w-12 rounded-2xl bg-slate-200/50 dark:bg-slate-900 border border-slate-200 dark:border-slate-800 flex items-center justify-center text-slate-400 dark:text-slate-400 shadow-2xs">
              <ImageOff className="h-6 w-6 stroke-[1.75]" />
            </div>
            <div className="space-y-0.5">
              <p className="text-xs font-semibold text-slate-700 dark:text-slate-200">
                {failedImages[current.id] ? 'Không thể tải hình ảnh' : current.fallbackText}
              </p>
              <p className="text-[11px] text-slate-400 dark:text-slate-500 max-w-sm mx-auto">
                {failedImages[current.id]
                  ? 'Tệp ảnh không tồn tại trên máy chủ hoặc đường dẫn ảnh không khả dụng'
                  : 'Chưa có dữ liệu hình ảnh ghi nhận từ camera'}
              </p>
            </div>
          </div>
        )}

        {/* Top Info Banner Overlay */}
        <div className="absolute top-2.5 left-2.5 right-2.5 flex items-center justify-between pointer-events-none">
          <div className="flex items-center gap-1.5 bg-white/95 dark:bg-slate-900/90 backdrop-blur-md px-2.5 py-1 rounded-md border border-slate-200/90 dark:border-slate-700/60 shadow-md">
            <span className={`text-[9px] font-extrabold text-white px-1.5 py-0.2 rounded ${current.tagColor}`}>
              {current.tag}
            </span>
            <span className="text-[11px] font-semibold text-slate-800 dark:text-slate-100">
              {current.label}
            </span>
          </div>

          <div className="bg-white/95 dark:bg-slate-900/90 backdrop-blur-md px-2.5 py-1 rounded-md border border-slate-200/90 dark:border-slate-700/60 text-[11px] text-slate-600 dark:text-slate-300 shadow-md font-mono hidden sm:block">
            {current.subtitle}
          </div>
        </div>

        {/* Left / Right Carousel Navigation Buttons */}
        <button
          type="button"
          onClick={handlePrevSlide}
          className="absolute left-2.5 top-1/2 -translate-y-1/2 h-8 w-8 rounded-full bg-white/90 dark:bg-slate-900/80 hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-800 dark:text-white flex items-center justify-center border border-slate-200 dark:border-slate-700/70 shadow-lg transition-all opacity-80 group-hover:opacity-100 hover:scale-110 cursor-pointer"
          title="Ảnh trước (Phím mũi tên Trái)"
        >
          <ChevronLeft className="h-4 w-4" />
        </button>

        <button
          type="button"
          onClick={handleNextSlide}
          className="absolute right-2.5 top-1/2 -translate-y-1/2 h-8 w-8 rounded-full bg-white/90 dark:bg-slate-900/80 hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-800 dark:text-white flex items-center justify-center border border-slate-200 dark:border-slate-700/70 shadow-lg transition-all opacity-80 group-hover:opacity-100 hover:scale-110 cursor-pointer"
          title="Ảnh tiếp theo (Phím mũi tên Phải)"
        >
          <ChevronRight className="h-4 w-4" />
        </button>

        {/* Dot Indicators */}
        <div className="absolute bottom-2.5 left-1/2 -translate-x-1/2 flex items-center gap-1.5 bg-white/80 dark:bg-slate-900/70 backdrop-blur-sm px-2 py-0.5 rounded-full border border-slate-200 dark:border-slate-700/50">
          {slides.map((_, idx) => (
            <button
              key={idx}
              type="button"
              onClick={() => setActiveSlide(idx)}
              className={`h-1.5 rounded-full transition-all cursor-pointer ${activeSlide === idx
                ? 'w-4 bg-blue-600 dark:bg-blue-500'
                : 'w-1.5 bg-slate-400 dark:bg-slate-200'
                }`}
              title={`Đến ảnh ${idx + 1}`}
            />
          ))}
        </div>
      </div>

      {/* Bottom Thumbnail Strip - Compact */}
      <div className="grid grid-cols-4 gap-2">
        {slides.map((slide, idx) => {
          const isThumbValid = slide.hasImg && !failedImages[slide.id];
          return (
            <button
              key={slide.id}
              type="button"
              onClick={() => setActiveSlide(idx)}
              className={`relative rounded-lg overflow-hidden border p-1 transition-all cursor-pointer text-left bg-slate-50 dark:bg-slate-950/60 ${activeSlide === idx
                ? 'border-blue-600 dark:border-blue-500 ring-2 ring-blue-500/30 bg-blue-50/50 dark:bg-slate-800/80 shadow-md'
                : 'border-slate-200 dark:border-slate-800 hover:border-slate-300 dark:hover:border-slate-700 opacity-70 hover:opacity-100'
                }`}
            >
              <div className="h-11 sm:h-12 w-full rounded bg-slate-200/50 dark:bg-slate-900 overflow-hidden flex items-center justify-center">
                {isThumbValid ? (
                  <img
                    src={slide.url}
                    alt={slide.label}
                    className="w-full h-full object-cover"
                    onError={() => {
                      setFailedImages((prev) => ({ ...prev, [slide.id]: true }));
                    }}
                  />
                ) : (
                  <div className="flex flex-col items-center justify-center text-slate-400 dark:text-slate-500 gap-0.5">
                    <ImageOff className="h-3.5 w-3.5 opacity-50" />
                    <span className="text-[9px] font-medium leading-none">
                      {failedImages[slide.id] ? 'Lỗi ảnh' : 'Chưa có'}
                    </span>
                  </div>
                )}
              </div>
              <div className="mt-1 flex items-center justify-between text-[10px] px-0.5">
                <span
                  className={`font-semibold truncate ${activeSlide === idx
                    ? 'text-blue-600 dark:text-blue-400'
                    : 'text-slate-500 dark:text-slate-400'
                    }`}
                >
                  {slide.tag}
                </span>
                <span className="text-[9px] text-slate-400 dark:text-slate-500 font-mono">
                  #{idx + 1}
                </span>
              </div>
            </button>
          );
        })}
      </div>
    </div>
  );
}
