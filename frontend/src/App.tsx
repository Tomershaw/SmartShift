import { ScheduleView } from "./features/scheduling/components/ScheduleView";
import "./App.css";

function App() {
  return (
    <div className="app">
      <header className="app-header">
        <h1>SmartShift</h1>
      </header>
      <main>
        <ScheduleView />
      </main>
    </div>
  );
}

export default App;
