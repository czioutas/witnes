import { toast } from "sonner";
import { AxiosError } from "axios";
import type { ApplicationProblemDetailsModel } from "../generated/api";

interface ApiCallOptions<T> {
  /** The API call to execute (must return a Promise) */
  apiCall: () => Promise<T>;
  /** Success message to show in toast (optional) */
  successMessage?: string;
  /** Callback to run on success (receives the API response data) */
  onSuccess?: (data: T) => void;
  /** Callback to run on error (receives the error message) */
  onError?: (errorMessage: string) => void;
  /** Whether to show error toast (default: true) */
  showErrorToast?: boolean;
  /** Whether to show success toast (default: true if successMessage provided) */
  showSuccessToast?: boolean;
}

interface ApiCallResult<T> {
  /** Whether the API call succeeded */
  success: boolean;
  /** The response data (only available on success) */
  data?: T;
  /** The error message (only available on error) */
  error?: string;
}

/**
 * Hook for handling API calls with automatic toast notifications.
 * Extracts error messages from ApplicationProblemDetailsModel's `detail` field.
 *
 * @example
 * ```typescript
 * const { handleApiCall } = useApiToast();
 *
 * await handleApiCall({
 *   apiCall: () => api.postV1Locations(request),
 *   successMessage: "Location created successfully",
 *   onSuccess: () => window.location.href = "/dashboard/locations"
 * });
 * ```
 */
export function useApiToast() {
  /**
   * Executes an API call and handles success/error toasts automatically.
   *
   * @param options - Configuration for the API call
   * @returns Result object with success status and data/error
   */
  const handleApiCall = async <T = any>(
    options: ApiCallOptions<T>
  ): Promise<ApiCallResult<T>> => {
    const {
      apiCall,
      successMessage,
      onSuccess,
      onError,
      showErrorToast = true,
      showSuccessToast = true,
    } = options;

    try {
      const response = await apiCall();

      // Show success toast if message provided and not disabled
      if (successMessage && showSuccessToast) {
        toast.success(successMessage);
      }

      // Call success callback if provided
      if (onSuccess) {
        onSuccess(response);
      }

      return {
        success: true,
        data: response,
      };
    } catch (error) {
      // Extract error message from ApplicationProblemDetailsModel
      let errorMessage = "An unexpected error occurred";

      if (error instanceof AxiosError && error.response?.data) {
        const problemDetails = error.response.data as ApplicationProblemDetailsModel;

        // Priority: detail > first error in errors array > fallback
        errorMessage =
          problemDetails.detail ||
          problemDetails.errors?.[0] ||
          errorMessage;
      } else if (error instanceof Error) {
        errorMessage = error.message;
      }

      // Show error toast if not disabled
      if (showErrorToast) {
        toast.error(errorMessage);
      }

      // Call error callback if provided
      if (onError) {
        onError(errorMessage);
      }

      return {
        success: false,
        error: errorMessage,
      };
    }
  };

  return { handleApiCall };
}
