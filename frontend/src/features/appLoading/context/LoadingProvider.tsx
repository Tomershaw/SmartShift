import { useState, useMemo, type ReactNode } from "react";
import { LoadingContext } from "./LoadingContext";
import { Spinner } from "../../scheduling/components/Spinner"; 

export function LoadingProvider({ children }: { children: ReactNode }) {
  // האם להציג את הלודר
  const [visible, setVisible] = useState(false);

  // הודעה שמוצגת מתחת לספינר
  const [message, setMessage] = useState<string | undefined>("טוען נתונים...");

  // API שיחשף לכל מי שמשתמש ב־useLoading()
  const api = useMemo(
    () => ({
      show: (msg?: string) => {
        if (msg) setMessage(msg);
        setVisible(true);
      },
      hide: () => setVisible(false),
    }),
    []
  );

  // מחזירים את ה־Provider עם כל הילדים של האפליקציה בתוכו
  return (
    <LoadingContext.Provider value={api}>
      {children}
      {visible && <Spinner message={message} />}
    </LoadingContext.Provider>
  );
}
