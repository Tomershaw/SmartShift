import { createContext } from "react";

type AppLoadingAPI = {
  show: (msg?: string) => void;
  hide: () => void;
};

// יוצרים Context ריק עם טיפוס (TypeScript) של AppLoadingAPI
export const LoadingContext = createContext<AppLoadingAPI>({
  show: () => {},
  hide: () => {},
});
