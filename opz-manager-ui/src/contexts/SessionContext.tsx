import React, { createContext, useState } from 'react';

interface SessionContextType {
  sessionId: string;
}

export const SessionContext = createContext<SessionContextType>({
  sessionId: '',
});

function generateSessionId(): string {
  // Simple UUID v4 without external dependency
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === 'x' ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}

export const SessionProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [sessionId] = useState<string>(() => {
    const existing = localStorage.getItem('anonymousSessionId');
    if (existing) return existing;
    const newId = generateSessionId();
    localStorage.setItem('anonymousSessionId', newId);
    return newId;
  });

  return (
    <SessionContext.Provider value={{ sessionId }}>
      {children}
    </SessionContext.Provider>
  );
};
