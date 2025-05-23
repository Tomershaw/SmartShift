import type { Employee } from "../types";

interface EmployeeCardProps {
  employee: Employee;
}

export const EmployeeCard = ({ employee }: EmployeeCardProps) => (
  <li key={employee.id}>
    {employee.name} (דירוג: {employee.priorityRating})
  </li>
);
