import { useContext } from 'react';
import { SessionContext } from '../contexts/SessionContext';

export function useSession() {
  return useContext(SessionContext);
}
