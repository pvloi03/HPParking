import type { ClientDto } from '@/types/client';
import { InfiniteSearchableSelect } from '@/components/ui/infinite-searchable-select';
import { clientApi } from '@/api/clientApi';

interface ClientSelectProps {
  value?: string;
  onValueChange: (clientId: string) => void;
  clients?: ClientDto[];
  placeholder?: string;
  disabled?: boolean;
}

export function ClientSelect({
  value,
  onValueChange,
  clients,
  placeholder = '-- Chọn chủ sở hữu phương tiện --',
  disabled = false,
}: ClientSelectProps) {
  return (
    <InfiniteSearchableSelect<ClientDto>
      queryKey={['clients-infinite-select']}
      fetchFn={(params) => clientApi.getPaged({ ...params, isActive: true })}
      fetchById={(id) => clientApi.getById(String(id))}
      value={value}
      onValueChange={onValueChange}
      placeholder={placeholder}
      disabled={disabled}
      selectedItems={clients}
      emptyMessage="Không tìm thấy nhân sự nào khớp"
      renderItem={(client) => (
        <div className="flex flex-col">
          <span className="font-semibold text-foreground">
            {client.name}{' '}
            {client.phoneNumber && (
              <span className="font-mono font-normal text-muted-foreground">
                ({client.phoneNumber})
              </span>
            )}
          </span>
          <span className="text-[10px] text-muted-foreground">
            Mã/CCCD: {client.code}
            {client.address ? ` • ${client.address}` : ''}
          </span>
        </div>
      )}
    />
  );
}
