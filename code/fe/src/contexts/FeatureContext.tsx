import {
  createContext,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from "react";
import { getWitnesServerAPI, type FeatureKey } from "../generated/api";
import { useAuth } from "./AuthContext";

interface FeatureContextType {
  features: FeatureKey[];
  isFeatureEnabled: (featureKey: FeatureKey) => boolean;
  isLoading: boolean;
  error: string | null;
  refetch: () => Promise<void>;
}

const FeatureContext = createContext<FeatureContextType | undefined>(undefined);

interface FeatureProviderProps {
  children: ReactNode;
}

export function FeatureProvider({ children }: FeatureProviderProps) {
  const auth = useAuth();
  const [features, setFeatures] = useState<FeatureKey[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchFeatures = async () => {
    try {
      setIsLoading(true);
      setError(null);
      const api = getWitnesServerAPI();
      const response = await api.getV1Features();
      setFeatures(response.data.features || []);
    } catch (err) {
      console.error("Failed to fetch features:", err);
      setError("Failed to load features");
      setFeatures([]);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    // Only fetch features when auth is ready and user is authenticated
    if (!auth.isLoading && auth.isAuthenticated) {
      fetchFeatures();
    } else if (!auth.isLoading && !auth.isAuthenticated) {
      // If not authenticated, stop loading
      setIsLoading(false);
    }
  }, [auth.isLoading, auth.isAuthenticated]);

  const isFeatureEnabled = (featureKey: FeatureKey): boolean => {
    return features.includes(featureKey);
  };

  const value: FeatureContextType = {
    features,
    isFeatureEnabled,
    isLoading,
    error,
    refetch: fetchFeatures,
  };

  return (
    <FeatureContext.Provider value={value}>{children}</FeatureContext.Provider>
  );
}

export function useFeatures(): FeatureContextType {
  const context = useContext(FeatureContext);
  if (context === undefined) {
    throw new Error("useFeatures must be used within a FeatureProvider");
  }
  return context;
}

export function useFeature(featureKey: FeatureKey): boolean {
  const { isFeatureEnabled } = useFeatures();
  return isFeatureEnabled(featureKey);
}
