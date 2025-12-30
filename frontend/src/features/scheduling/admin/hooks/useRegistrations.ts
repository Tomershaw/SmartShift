import { useCallback, useEffect, useMemo, useState } from "react";
import {
  getRegistrationsSnapshot,
  getDayRegistrants,
  type RegistrationsSnapshotDayDto,
  type DayRegistrantNameDto,
} from "../api/registrationsApi";

export type RegistrationStatusFilter =
  | "All"
  | "Pending"
  | "Approved"
  | "Rejected"
  | "Cancelled";

// ניגזר סוג הסטטוס האמיתי מהפילטר (בלי "All")
type RegistrationStatus = Exclude<RegistrationStatusFilter, "All">;

function toIlISO(date: Date): string {
  return new Intl.DateTimeFormat("en-CA", {
    timeZone: "Asia/Jerusalem",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).format(date);
}

function getSunThuWindow(pivot: Date): { from: string; to: string } {
  const base = new Date(
    new Intl.DateTimeFormat("en-CA", { timeZone: "Asia/Jerusalem" }).format(
      pivot
    )
  );
  const day = base.getDay(); // 0..6
  const sunday = new Date(base);
  sunday.setDate(base.getDate() - day);
  const thursday = new Date(sunday);
  thursday.setDate(sunday.getDate() + 4);
  return { from: toIlISO(sunday), to: toIlISO(thursday) };
}

export function useRegistrations(pivotDate: Date = new Date()) {
  const [snapshot, setSnapshot] = useState<RegistrationsSnapshotDayDto[]>([]);
  const [selectedDate, setSelectedDate] = useState<string>("");
  const [status, setStatus] = useState<RegistrationStatusFilter>("Pending");

  const [names, setNames] = useState<DayRegistrantNameDto[]>([]);
  const [skip, setSkip] = useState(0);
  const [take] = useState(50);

  const [loadingSnapshot, setLoadingSnapshot] = useState(false);
  const [loadingNames, setLoadingNames] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const { from, to } = useMemo(() => getSunThuWindow(pivotDate), [pivotDate]);

  const hasMore = useMemo(
    () => names.length > 0 && names.length % take === 0,
    [names.length, take]
  );

  const loadSnapshot = useCallback(async (): Promise<void> => {
    setLoadingSnapshot(true);
    setError(null);
    try {
      const data = await getRegistrationsSnapshot({ from, to });
      setSnapshot(data ?? []);
      const first =
        data?.find(d => d.registeredCount > 0)?.date ?? data?.[0]?.date ?? from;
      setSelectedDate(prev => prev || first);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "שגיאה בטעינת תמונת המצב");
    } finally {
      setLoadingSnapshot(false);
    }
  }, [from, to]);

  const loadNames = useCallback(
    async (reset: boolean = true): Promise<void> => {
      if (!selectedDate) return;
      setLoadingNames(true);
      setError(null);
      try {
        const effectiveStatus: RegistrationStatus | undefined =
          status === "All" ? undefined : status;

        const pageSkip = reset ? 0 : skip;

        const data = await getDayRegistrants({
          date: selectedDate,
          status: effectiveStatus,
          skip: pageSkip,
          take,
        });
        console.log("DAY REGISTRANTS FROM API:", data);
        setNames(prev => (reset ? data : [...prev, ...data]));
        if (reset) setSkip(take);
        else setSkip(s => s + take);
      } catch (e: unknown) {
        setError(e instanceof Error ? e.message : "שגיאה בטעינת רשימת הנרשמים");
      } finally {
        setLoadingNames(false);
      }
    },
    [selectedDate, status, skip, take]
  );

  useEffect(() => {
    void loadSnapshot();
  }, [loadSnapshot]);

  useEffect(() => {
    if (!selectedDate) return;
    void loadNames(true);
  }, [selectedDate, status, loadNames]);

  const selectDay = useCallback((date: string) => setSelectedDate(date), []);
  const changeStatus = useCallback(
    (next: RegistrationStatusFilter) => setStatus(next),
    []
  );
  const refreshDay = useCallback(() => {
    void loadNames(true);
  }, [loadNames]);
  const loadMore = useCallback(() => {
    if (hasMore && !loadingNames) {
      void loadNames(false);
    }
  }, [hasMore, loadingNames, loadNames]);

  const daysPills = useMemo(
    () =>
      snapshot.map(d => ({
        date: d.date,
        required: d.requiredCount,
        registered: d.registeredCount,
        pending: d.pendingCount,
        approved: d.approvedCount,
        isSelected: d.date === selectedDate,
      })),
    [snapshot, selectedDate]
  );

  const selectedDaySummary = useMemo(() => {
    const d = snapshot.find(x => x.date === selectedDate);
    if (!d) return null;
    return {
      date: d.date,
      required: d.requiredCount,
      registered: d.registeredCount,
      pending: d.pendingCount,
      approved: d.approvedCount,
      rejected: d.rejectedCount,
      cancelled: d.cancelledCount,
    };
  }, [snapshot, selectedDate]);

  return {
    from,
    to,
    snapshot,
    daysPills,
    selectedDate,
    selectedDaySummary,
    names,
    status,
    loadingSnapshot,
    loadingNames,
    hasMore,
    error,
    selectDay,
    changeStatus,
    refreshDay,
    loadMore,
    reloadSnapshot: loadSnapshot,
  };
}
