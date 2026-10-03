import { useEffect, useRef, useCallback } from 'react';

export interface UseIntersectionSentinelOptions {
  /** Có kích hoạt observer hay không (ví dụ: hasNextPage && !isFetchingNextPage) */
  enabled?: boolean;
  /** Hàm callback được gọi khi phần tử sentinel xuất hiện trong khung nhìn */
  onIntersect: () => void;
  /** Khoảng cách đệm trước khi phần tử chạm vào khung nhìn (mặc định '50px') */
  rootMargin?: string;
  /** Ngưỡng hiển thị (0.0 đến 1.0, mặc định 0.1) */
  threshold?: number;
  /** Phần tử cha làm khung cuộn (mặc định là null - viewport hoặc container gần nhất) */
  root?: HTMLElement | null;
}

/**
 * Hook theo dõi một phần tử sentinel ở cuối danh sách:
 * Khi người dùng cuộn đến gần cuối, tự động kích hoạt callback `onIntersect()` để tải trang tiếp theo.
 */
export function useIntersectionSentinel({
  enabled = true,
  onIntersect,
  rootMargin = '50px',
  threshold = 0.1,
  root = null,
}: UseIntersectionSentinelOptions) {
  const targetRef = useRef<HTMLElement | null>(null);
  const onIntersectRef = useRef(onIntersect);

  // Giữ tham chiếu mới nhất của onIntersect để tránh re-subscribe không cần thiết
  useEffect(() => {
    onIntersectRef.current = onIntersect;
  }, [onIntersect]);

  const setTargetRef = useCallback(
    (node: HTMLElement | null) => {
      targetRef.current = node;
    },
    []
  );

  useEffect(() => {
    if (!enabled || !targetRef.current || typeof IntersectionObserver === 'undefined') {
      return;
    }

    const observer = new IntersectionObserver(
      (entries) => {
        const firstEntry = entries[0];
        if (firstEntry && firstEntry.isIntersecting) {
          onIntersectRef.current();
        }
      },
      {
        root,
        rootMargin,
        threshold,
      }
    );

    const currentTarget = targetRef.current;
    observer.observe(currentTarget);

    return () => {
      if (currentTarget) {
        observer.unobserve(currentTarget);
      }
      observer.disconnect();
    };
  }, [enabled, rootMargin, threshold, root]);

  return { sentinelRef: setTargetRef };
}
