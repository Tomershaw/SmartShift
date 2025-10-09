import { Loader2 } from "lucide-react";

export function Spinner({ message }: { message?: string }) {
  return (
    <div className="fixed inset-0 z-50 flex flex-col items-center justify-center bg-white/70 backdrop-blur-sm">
      <Loader2 className="h-12 w-12 animate-spin text-sky-600" />
      {message && (
        <p className="mt-4 text-slate-700 font-medium text-sm">{message}</p>
      )}
    </div>
  );
}
