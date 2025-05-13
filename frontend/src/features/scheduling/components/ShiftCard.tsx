interface ShiftCardProps {
    shift: {
      id: string;
      startTime: string;
      endTime: string;
      assignedEmployeeId?: string;
    };
    employees: { id: string; name: string }[];
  }
  
  export const ShiftCard = ({ shift, employees }: ShiftCardProps) => {
    const { id, startTime, endTime, assignedEmployeeId } = shift;
    const assignedEmployee = employees.find(e => e.id === assignedEmployeeId)?.name || "לא מוקצה";
  
    return (
      <li key={id}>
        {new Date(startTime).toLocaleString()} - {new Date(endTime).toLocaleString()} ({assignedEmployee})
   </li>
  );
  };