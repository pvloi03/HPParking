import { SessionsManager } from '@/components/parkingSessions/SessionsManager';
import { LaneTargetType } from '@/types/infrastructure';

export function PedestrianSessionsPage() {
  return <SessionsManager targetType={LaneTargetType.Pedestrian} />;
}
