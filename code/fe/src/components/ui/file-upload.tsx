import { useState, useRef } from "react";
import { Button } from "./button";
import { Upload, X, File, Download, Loader2 } from "lucide-react";

export interface UploadedFile {
  id: string;
  original_file_name: string;
  file_size_bytes: number;
  mime_type: string;
}

interface FileUploadProps {
  /** Current uploaded file info */
  currentFile?: UploadedFile | null;
  /** Called when file is selected and uploaded successfully */
  onFileUploaded: (file: UploadedFile) => void;
  /** Called when user wants to remove the file */
  onFileRemoved: () => void;
  /** Called when user wants to download the file */
  onFileDownload?: (fileId: string) => void;
  /** Allowed file extensions (e.g., [".pdf", ".jpg"]) */
  accept?: string;
  /** Maximum file size in bytes */
  maxSizeBytes?: number;
  /** Whether the upload is disabled */
  disabled?: boolean;
  /** Upload function that returns the uploaded file info */
  uploadFn: (file: File) => Promise<UploadedFile>;
  /** Custom label for the upload button */
  uploadLabel?: string;
  /** Show upload status message */
  showUploadStatus?: boolean;
}

export function FileUpload({
  currentFile,
  onFileUploaded,
  onFileRemoved,
  onFileDownload,
  accept,
  maxSizeBytes,
  disabled = false,
  uploadFn,
  uploadLabel = "Upload File",
  showUploadStatus = false,
}: FileUploadProps) {
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [uploadSuccess, setUploadSuccess] = useState(false);
  const [removeSuccess, setRemoveSuccess] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleFileSelect = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    setError(null);
    setUploadSuccess(false);

    // Validate file size
    if (maxSizeBytes && file.size > maxSizeBytes) {
      const maxSizeMB = maxSizeBytes / 1024 / 1024;
      setError(`File size exceeds ${maxSizeMB}MB limit`);
      return;
    }

    // Validate file type
    if (accept) {
      const extension = `.${file.name.split('.').pop()?.toLowerCase()}`;
      const allowedExtensions = accept.split(',').map(ext => ext.trim().toLowerCase());
      if (!allowedExtensions.includes(extension)) {
        setError(`File type not allowed. Accepted: ${accept}`);
        return;
      }
    }

    try {
      setUploading(true);
      const uploadedFile = await uploadFn(file);
      onFileUploaded(uploadedFile);
      setUploadSuccess(true);

      // Clear success message after 3 seconds
      setTimeout(() => setUploadSuccess(false), 3000);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Upload failed');
    } finally {
      setUploading(false);
      // Reset file input
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    }
  };

  const handleRemove = () => {
    setError(null);
    setUploadSuccess(false);
    setRemoveSuccess(true);
    onFileRemoved();

    // Clear success message after 3 seconds
    setTimeout(() => setRemoveSuccess(false), 3000);
  };

  const formatFileSize = (bytes: number) => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  };

  return (
    <div className="space-y-2">
      {/* Current File Display */}
      {currentFile && (
        <div className="flex items-center gap-2 p-3 border rounded-md bg-muted/50">
          <File className="h-4 w-4 text-muted-foreground" />
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium truncate">{currentFile.original_file_name}</p>
            <p className="text-xs text-muted-foreground">
              {formatFileSize(currentFile.file_size_bytes)}
            </p>
          </div>
          <div className="flex gap-1">
            {onFileDownload && (
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => onFileDownload(currentFile.id)}
                disabled={disabled}
              >
                <Download className="h-4 w-4" />
              </Button>
            )}
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={handleRemove}
              disabled={disabled}
              className="text-destructive hover:text-destructive"
            >
              <X className="h-4 w-4" />
            </Button>
          </div>
        </div>
      )}

      {/* Upload Button */}
      {!currentFile && (
        <div className="space-y-2">
          <input
            ref={fileInputRef}
            type="file"
            accept={accept}
            onChange={handleFileSelect}
            disabled={disabled || uploading}
            className="hidden"
          />
          <Button
            type="button"
            variant="outline"
            onClick={() => fileInputRef.current?.click()}
            disabled={disabled || uploading}
            className="w-full"
          >
            {uploading ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Uploading...
              </>
            ) : (
              <>
                <Upload className="mr-2 h-4 w-4" />
                {uploadLabel}
              </>
            )}
          </Button>
        </div>
      )}

      {/* Status Messages */}
      {error && (
        <p className="text-sm text-destructive">{error}</p>
      )}
      {showUploadStatus && uploadSuccess && (
        <p className="text-sm text-green-600 dark:text-green-400">
          Uploaded successfully! Please save to apply changes.
        </p>
      )}
      {showUploadStatus && removeSuccess && (
        <p className="text-sm text-amber-600 dark:text-amber-400">
          File removed! Please save to apply changes.
        </p>
      )}
    </div>
  );
}
