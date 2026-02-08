import type { ReactNode } from "react";
import { useState, useImperativeHandle, forwardRef } from "react";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "./alert-dialog";

interface ConfirmDialogProps<T = void> {
  title?: string;
  description?: ReactNode | ((data?: T) => ReactNode);
  confirmText?: string;
  cancelText?: string;
  onConfirm: (data?: T) => Promise<void> | void;
  variant?: "default" | "destructive";
}

export interface ConfirmDialogRef<T = void> {
  open: (data?: T) => void;
}

function ConfirmDialogComponent<T = void>(
  {
    title = "Are you sure?",
    description = "This action cannot be undone.",
    confirmText = "Confirm",
    cancelText = "Cancel",
    onConfirm,
    variant = "default",
  }: ConfirmDialogProps<T>,
  ref: React.Ref<ConfirmDialogRef<T>>
) {
  const [isOpen, setIsOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [data, setData] = useState<T | undefined>(undefined);

  useImperativeHandle(ref, () => ({
    open: (confirmData?: T) => {
      setData(confirmData);
      setIsOpen(true);
    },
  }));

  const handleConfirm = async () => {
    setIsLoading(true);
    try {
      await onConfirm(data);
      setIsOpen(false);
      setData(undefined);
    } catch (error) {
      // Error is handled by the caller (e.g., via handleApiCall)
    } finally {
      setIsLoading(false);
    }
  };

  const handleCancel = () => {
    setIsOpen(false);
    setData(undefined);
  };

  const renderedDescription = typeof description === "function" ? description(data) : description;

  return (
    <AlertDialog open={isOpen} onOpenChange={setIsOpen}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{title}</AlertDialogTitle>
          <AlertDialogDescription>{renderedDescription}</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isLoading} onClick={handleCancel}>
            {cancelText}
          </AlertDialogCancel>
          <AlertDialogAction
            onClick={handleConfirm}
            disabled={isLoading}
            className={variant === "destructive" ? "bg-destructive hover:bg-destructive/90" : ""}
          >
            {isLoading ? "Loading..." : confirmText}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}

export const ConfirmDialog = forwardRef(ConfirmDialogComponent) as <T = void>(
  props: ConfirmDialogProps<T> & { ref?: React.Ref<ConfirmDialogRef<T>> }
) => ReturnType<typeof ConfirmDialogComponent>;
