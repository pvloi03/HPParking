import { useState, useMemo } from 'react';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Input } from '@/components/ui/input';
import { Search } from 'lucide-react';
import type { ClientDto } from '@/types/client';

interface ClientSelectProps {
  value?: string;
  onValueChange: (clientId: string) => void;
  clients: ClientDto[];
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
  const [search, setSearch] = useState('');

  const filteredClients = useMemo(() => {
    if (!search.trim()) return clients;
    const q = search.toLowerCase().trim();
    return clients.filter(
      (c) =>
        c.fullName.toLowerCase().includes(q) ||
        c.phoneNumber.includes(q) ||
        c.identityNumber.includes(q) ||
        c.code.toLowerCase().includes(q)
    );
  }, [clients, search]);

  return (
    <div className="space-y-1.5">
      <Select
        value={value || ''}
        onValueChange={onValueChange}
        disabled={disabled}
      >
        <SelectTrigger className="text-xs h-9">
          <SelectValue placeholder={placeholder} />
        </SelectTrigger>
        <SelectContent className="max-h-60">
          <div className="p-1.5 border-b border-border sticky top-0 bg-popover z-10">
            <div className="relative">
              <Search className="absolute left-2 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground pointer-events-none" />
              <Input
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Tìm theo tên, SĐT, CCCD..."
                className="pl-7 h-7 text-xs"
                onClick={(e) => e.stopPropagation()}
                onKeyDown={(e) => e.stopPropagation()}
              />
            </div>
          </div>
          {filteredClients.length === 0 ? (
            <div className="py-4 text-center text-xs text-muted-foreground">
              Không tìm thấy khách hàng nào khớp
            </div>
          ) : (
            filteredClients.map((client) => (
              <SelectItem key={client.id} value={client.id} className="text-xs py-1.5">
                <div className="flex flex-col">
                  <span className="font-semibold text-foreground">
                    {client.fullName}{' '}
                    <span className="font-mono font-normal text-muted-foreground">
                      ({client.phoneNumber})
                    </span>
                  </span>
                  <span className="text-[10px] text-muted-foreground">
                    Mã: {client.code} • CCCD: {client.identityNumber}
                    {client.companyName ? ` • ${client.companyName}` : ''}
                  </span>
                </div>
              </SelectItem>
            ))
          )}
        </SelectContent>
      </Select>
    </div>
  );
}
