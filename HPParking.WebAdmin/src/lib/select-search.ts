import { useMemo, useState } from 'react';

/**
 * Ngưỡng số lượng phần tử mặc định để kích hoạt ô tìm kiếm trong Select
 */
export const DEFAULT_SELECT_SEARCH_THRESHOLD = 10;

/**
 * Thông tin trường tìm kiếm được tự động phát hiện
 */
export interface IdentifiedSearchField<T = any> {
  field: keyof T & string;
  label: string;
  placeholder: string;
  isCode: boolean;
}

/**
 * Danh sách tên trường đặc thù đại diện cho "MÃ" (Code), sắp xếp theo độ ưu tiên
 */
const CODE_FIELD_PATTERNS: Array<{ regex: RegExp; label: string }> = [
  { regex: /^code$/i, label: 'mã' },
  { regex: /^(route|gate|lane|device|client|company|department|contractor|user|vehicle|card|staff|employee)code$/i, label: 'mã' },
  { regex: /^cardnumber$/i, label: 'mã thẻ' },
  { regex: /^(platenumber|licenseplate|plate)$/i, label: 'biển số' },
  { regex: /^username$/i, label: 'tên đăng nhập' },
  { regex: /^(serialnumber|serial)$/i, label: 'số serial' },
  { regex: /^(barcode|sku)$/i, label: 'mã vạch/SKU' },
  { regex: /code$/i, label: 'mã' },
  { regex: /code/i, label: 'mã' },
];

/**
 * Danh sách các trường có độ nhận diện cao nhất khi KHÔNG CÓ MÃ (ngoài id), sắp xếp theo thứ tự ưu tiên
 */
const IDENTIFYING_NON_CODE_FIELDS: Array<{ regex: RegExp; label: string }> = [
  { regex: /^(name|fullname|displayname|clientname|companyname|gatename|routename|lanename|devicename)$/i, label: 'tên' },
  { regex: /^name/i, label: 'tên' },
  { regex: /^(title)$/i, label: 'tiêu đề' },
  { regex: /^(label)$/i, label: 'nhãn' },
  { regex: /^(phonenumber|phone|mobile)$/i, label: 'số điện thoại' },
  { regex: /^(email)$/i, label: 'email' },
  { regex: /^(ipaddress|ip|macaddress|mac)$/i, label: 'địa chỉ IP' },
  { regex: /^(identitycard|idcard|cccd|cmnd)$/i, label: 'số CCCD' },
  { regex: /^(taxcode|taxnumber)$/i, label: 'mã số thuế' },
  { regex: /^(description|desc|note)$/i, label: 'mô tả' },
];

/**
 * Các trường kỹ thuật hoặc ID bị bỏ qua khi tìm trường nhận diện
 */
const IGNORED_FIELD_KEYS = new Set([
  'id',
  '_id',
  'key',
  'uuid',
  'isdeleted',
  'isactive',
  'isshared',
  'status',
  'type',
  'targettype',
  'gender',
  'role',
  'roles',
  'permissions',
  'createdat',
  'updatedat',
  'created_at',
  'updated_at',
  'version',
  '__v',
  'password',
  'passwordhash',
  'salt',
  'token',
]);

/**
 * Chuẩn hóa chuỗi tiếng Việt (bỏ dấu, chuyển chữ thường) để tìm kiếm không dấu / có dấu đều khớp
 */
export function normalizeVietnamese(str: string): string {
  if (!str) return '';
  return str
    .toString()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/đ/g, 'd')
    .replace(/Đ/g, 'D')
    .toLowerCase()
    .trim();
}

/**
 * Kiểm tra xem danh sách có nhiều hơn ngưỡng cho phép (mặc định > 10 phần tử) để hiển thị ô tìm kiếm hay không
 */
export function shouldShowSelectSearch(
  totalCount: number,
  threshold: number = DEFAULT_SELECT_SEARCH_THRESHOLD
): boolean {
  return totalCount > threshold;
}

/**
 * Tự động phân tích một đối tượng mẫu (hoặc danh sách đối tượng) để tìm trường tìm kiếm tối ưu:
 * 1. Ưu tiên 1: Tìm theo "MÃ" (code, plateNumber, cardNumber, routeCode, username...)
 * 2. Ưu tiên 2: Nếu không có mã, tìm theo trường có độ nhận diện lớn nhất ngoài ID (name, fullName, title, phoneNumber...)
 */
export function getSelectSearchField<T = any>(
  sample: T | T[] | undefined | null
): IdentifiedSearchField<T> {
  const defaultFallback: IdentifiedSearchField<T> = {
    field: 'name' as keyof T & string,
    label: 'tên/mã',
    placeholder: 'Tìm kiếm...',
    isCode: false,
  };

  if (!sample) return defaultFallback;

  const item: any = Array.isArray(sample)
    ? sample.find((x) => x && typeof x === 'object') || sample[0]
    : sample;

  if (!item || typeof item !== 'object') {
    return defaultFallback;
  }

  const keys = Object.keys(item);
  if (keys.length === 0) return defaultFallback;

  // Lọc các key hợp lệ (không phải ID, boolean, object/array con, hoặc trường hệ thống)
  const validCandidateKeys = keys.filter((key) => {
    const lowerKey = key.toLowerCase();
    if (IGNORED_FIELD_KEYS.has(lowerKey)) return false;
    if (lowerKey.endsWith('id') && lowerKey !== 'idcard') return false;

    const val = item[key];
    if (val === null || val === undefined) return true;
    const type = typeof val;
    return type === 'string' || type === 'number';
  });

  // BƯỚC 1: Tìm trường "MÃ" theo các pattern đặc thù
  for (const pattern of CODE_FIELD_PATTERNS) {
    const foundKey = validCandidateKeys.find((k) => pattern.regex.test(k));
    if (foundKey) {
      return {
        field: foundKey as keyof T & string,
        label: pattern.label,
        placeholder: `Tìm theo ${pattern.label}...`,
        isCode: true,
      };
    }
  }

  // BƯỚC 2: Nếu không có mã, tìm trường có ĐỘ NHẬN DIỆN LỚN NHẤT NGOÀI ID
  for (const pattern of IDENTIFYING_NON_CODE_FIELDS) {
    const foundKey = validCandidateKeys.find((k) => pattern.regex.test(k));
    if (foundKey) {
      return {
        field: foundKey as keyof T & string,
        label: pattern.label,
        placeholder: `Tìm theo ${pattern.label}...`,
        isCode: false,
      };
    }
  }

  // BƯỚC 3: Nếu vẫn chưa thấy, lấy trường dạng string đầu tiên còn lại
  const firstStringKey = validCandidateKeys.find((k) => typeof item[k] === 'string');
  if (firstStringKey) {
    return {
      field: firstStringKey as keyof T & string,
      label: firstStringKey,
      placeholder: `Tìm theo ${firstStringKey}...`,
      isCode: false,
    };
  }

  return defaultFallback;
}

/**
 * Lọc danh sách phần tử dựa trên từ khóa tìm kiếm và trường nhận diện đã xác định
 */
export function filterSelectItems<T = any>(
  items: T[],
  searchQuery: string,
  targetField?: keyof T & string,
  extraSearchFields: Array<keyof T & string> = []
): T[] {
  if (!items || !Array.isArray(items)) return [];
  const query = normalizeVietnamese(searchQuery);
  if (!query) return items;

  // Xác định trường tìm kiếm nếu chưa truyền vào
  const primaryField = targetField || getSelectSearchField(items).field;

  return items.filter((item: any) => {
    if (!item) return false;

    // 1. Kiểm tra trường nhận diện chính
    if (primaryField && item[primaryField] !== undefined && item[primaryField] !== null) {
      const fieldVal = normalizeVietnamese(String(item[primaryField]));
      if (fieldVal.includes(query)) return true;
    }

    // 2. Nếu trường chính là mã (code), tự động kiểm tra thêm name/fullName để trải nghiệm tìm kiếm tự nhiên
    const secondaryFields = [
      ...extraSearchFields,
      'name',
      'fullName',
      'title',
      'label',
      'phoneNumber',
    ];

    for (const secKey of secondaryFields) {
      if (secKey !== primaryField && item[secKey] !== undefined && item[secKey] !== null) {
        const val = normalizeVietnamese(String(item[secKey]));
        if (val.includes(query)) return true;
      }
    }

    return false;
  });
}

/**
 * Hook dùng chung cho việc tìm kiếm trong Select:
 * - Tự động phát hiện trường tìm kiếm (mã hoặc độ nhận diện lớn nhất ngoài id)
 * - Tự động kiểm tra điều kiện > 10 phần tử
 * - Tự động lọc danh sách theo từ khóa
 */
export function useSelectSearch<T = any>(
  items: T[],
  options?: {
    threshold?: number;
    searchField?: keyof T & string;
    customPlaceholder?: string;
    extraSearchFields?: Array<keyof T & string>;
  }
) {
  const [search, setSearch] = useState('');
  const threshold = options?.threshold ?? DEFAULT_SELECT_SEARCH_THRESHOLD;

  // Tự động nhận diện trường tìm kiếm từ mảng items
  const identified = useMemo(() => {
    if (options?.searchField) {
      return {
        field: options.searchField,
        label: String(options.searchField),
        placeholder: options.customPlaceholder || `Tìm theo ${String(options.searchField)}...`,
        isCode: /code|plate|card|sku|serial/i.test(String(options.searchField)),
      };
    }
    const detected = getSelectSearchField(items);
    if (options?.customPlaceholder) {
      return { ...detected, placeholder: options.customPlaceholder };
    }
    return detected;
  }, [items, options?.searchField, options?.customPlaceholder]);

  // Kiểm tra điều kiện số lượng phần tử > 10
  const showSearch = useMemo(() => {
    return shouldShowSelectSearch(items?.length || 0, threshold);
  }, [items?.length, threshold]);

  // Danh sách đã được lọc theo từ khóa
  const filteredItems = useMemo(() => {
    if (!showSearch || !search.trim()) return items;
    return filterSelectItems(items, search, identified.field, options?.extraSearchFields);
  }, [items, search, showSearch, identified.field, options?.extraSearchFields]);

  return {
    search,
    setSearch,
    filteredItems,
    showSearch,
    searchField: identified.field,
    placeholder: identified.placeholder,
    label: identified.label,
    isCode: identified.isCode,
    resetSearch: () => setSearch(''),
  };
}
