export {};

declare global {
  interface Window {
    Witnes?: {
      identify: (userId: string) => void;
    };
    witnesConfig?: {
      projectKey: string;
      userId?: string;
    };
  }
}
