// src/features/scheduling/admin/components/assignments/WeekGrid.tsx
import {
    DndContext,
    useDraggable,
    useDroppable,
    closestCenter,
    MouseSensor,
    TouchSensor,
    useSensor,
    useSensors,
  } from "@dnd-kit/core";
  import type { DragEndEvent } from "@dnd-kit/core";
  import { CSS } from "@dnd-kit/utilities";
  import type {
    WeeklyAssignment,
    DayAssignments,
    EmployeeMini,
    ShiftType,
  } from "../../types/assignments";
  
  /* ===== Draggable employee ===== */
  function DraggableEmp({
    emp,
    fromDay,
    fromType,
  }: {
    emp: EmployeeMini;
    fromDay: string;
    fromType: ShiftType;
  }) {
    const { attributes, listeners, setNodeRef, transform, isDragging } =
      useDraggable({
        // מזהה ייחודי לפי יום+טייפ+עובד כדי למנוע גרירה "מכל הימים"
        id: `slot-${fromDay}-${fromType}-${emp.id}`,
        data: { emp, from: { day: fromDay, type: fromType } },
      });
    const style = {
      transform: CSS.Translate.toString(transform),
      opacity: isDragging ? 0.6 : 1,
    };
    return (
      <li
        ref={setNodeRef}
        {...attributes}
        {...listeners}
        style={style}
        className="rounded-lg bg-white border px-2 py-1 mt-1 cursor-grab active:cursor-grabbing text-sm"
      >
        {emp.name}
      </li>
    );
  }
  
  /* ===== Droppable zone ===== */
  function DropZone({
    id,
    title,
    colorClass,
    children,
  }: {
    id: string;
    title: string;
    colorClass: string;
    children: React.ReactNode;
  }) {
    const { isOver, setNodeRef } = useDroppable({ id });
    return (
      <div
        ref={setNodeRef}
        className={`rounded-xl border bg-slate-50 transition ${
          isOver ? "ring-2 ring-emerald-300" : ""
        }`}
      >
        <div className={`px-3 py-1.5 text-xs font-semibold ${colorClass}`}>
          {title}
        </div>
        <ul className="px-3 pb-2">{children}</ul>
      </div>
    );
  }
  
  /* ===== Week grid (controlled) ===== */
  type Props = {
    value: WeeklyAssignment;
    onChange: (next: WeeklyAssignment) => void;
  };
  
  export default function WeekGrid({ value, onChange }: Props) {
    const sensors = useSensors(
      useSensor(MouseSensor, { activationConstraint: { distance: 5 } }),
      useSensor(TouchSensor, {
        activationConstraint: { delay: 150, tolerance: 5 },
      })
    );
  
    function moveEmp(
      emp: EmployeeMini,
      from: { day: string; type: ShiftType },
      to: { day: string; type: ShiftType }
    ) {
      // אם יש לך כלל עסקי ל-Early/Regular תוכל להוסיף כאן
  
      const next: WeeklyAssignment = {
        weekStart: value.weekStart,
        days: value.days.map<DayAssignments>((d) => ({
          ...d,
          early: [...d.early],
          regular: [...d.regular],
        })),
      };
  
      const fromDay = next.days.find((d) => d.iso === from.day);
      const toDay = next.days.find((d) => d.iso === to.day);
      if (!fromDay || !toDay) return;
  
      const fromArr = from.type === "early" ? fromDay.early : fromDay.regular;
      const toArr = to.type === "early" ? toDay.early : toDay.regular;
  
      const idx = fromArr.findIndex((e) => e.id === emp.id);
      if (idx >= 0) fromArr.splice(idx, 1);
  
      if (!toArr.some((e) => e.id === emp.id)) toArr.push(emp);
  
      onChange(next);
    }
  
    function onDragEnd(e: DragEndEvent) {
      const { active, over } = e;
      if (!over) return;
  
      const payload = active.data.current as
        | { emp: EmployeeMini; from: { day: string; type: ShiftType } }
        | undefined;
      if (!payload) return;
  
      const [toDay, toType] = String(over.id).split("|") as [string, ShiftType];
      const from = payload.from;
      if (from.day === toDay && from.type === toType) return;
  
      moveEmp(payload.emp, from, { day: toDay, type: toType });
    }
  
    return (
      <DndContext
        sensors={sensors}
        collisionDetection={closestCenter}
        onDragEnd={onDragEnd}
      >
        <div dir="rtl" className="w-full">
          <div className="grid grid-cols-1 gap-4 md:grid-cols-5">
            {value.days.map((day) => (
              <section
                key={day.iso}
                className="rounded-2xl border border-slate-200 bg-white p-3"
              >
                <header className="mb-2">
                  <h3 className="text-sm font-bold text-slate-800">
                    {day.title ?? day.iso}
                  </h3>
                  <p className="text-xs text-slate-500">{day.iso}</p>
                </header>
  
                <div className="mb-3">
                  <DropZone
                    id={`${day.iso}|early`}
                    title="Early"
                    colorClass="text-emerald-700"
                  >
                    {day.early.length === 0 && (
                      <li className="text-slate-400 text-xs">אין מוקדמים</li>
                    )}
                    {day.early.map((e) => (
                      <DraggableEmp
                        key={`early-${day.iso}-${e.id}`}
                        emp={e}
                        fromDay={day.iso}
                        fromType="early"
                      />
                    ))}
                  </DropZone>
                </div>
  
                <DropZone
                  id={`${day.iso}|regular`}
                  title="Regular"
                  colorClass="text-sky-700"
                >
                  {day.regular.length === 0 && (
                    <li className="text-slate-400 text-xs">אין רגילים</li>
                  )}
                  {day.regular.map((e) => (
                    <DraggableEmp
                      key={`regular-${day.iso}-${e.id}`}
                      emp={e}
                      fromDay={day.iso}
                      fromType="regular"
                    />
                  ))}
                </DropZone>
              </section>
            ))}
          </div>
        </div>
      </DndContext>
    );
  }
  