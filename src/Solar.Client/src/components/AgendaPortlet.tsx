import { useState } from 'react';

export const AgendaPortlet = () => {
  const [selectedDay, setSelectedDay] = useState(18);

  const activeDays = [3, 10, 17, 18, 24, 26, 31];

  // Calendário de Agosto 2026 (Começa no Sábado 1)
  const calendarRows = [
    [null, null, null, null, null, null, 1],
    [2, 3, 4, 5, 6, 7, 8],
    [9, 10, 11, 12, 13, 14, 15],
    [16, 17, 18, 19, 20, 21, 22],
    [23, 24, 25, 26, 27, 28, 29],
    [30, 31, null, null, null, null, null]
  ];

  return (
    <div className="solar-portlet-card agenda-card">
      <div className="portlet-simple-header">
        <span className="portlet-title-icon">📅</span>
        <strong>Agenda</strong>
      </div>

      <div className="agenda-calendar-container">
        <div className="calendar-month-selector">
          <button type="button" className="cal-arrow">◀</button>
          <strong>Agosto 2026</strong>
          <button type="button" className="cal-arrow">▶</button>
        </div>

        <table className="mini-calendar-table">
          <thead>
            <tr>
              <th>Dom</th>
              <th>Seg</th>
              <th>Ter</th>
              <th>Qua</th>
              <th>Qui</th>
              <th>Sex</th>
              <th>Sab</th>
            </tr>
          </thead>
          <tbody>
            {calendarRows.map((row, rIdx) => (
              <tr key={rIdx}>
                {row.map((day, dIdx) => {
                  if (day === null) {
                    return <td key={dIdx} className="empty-day"></td>;
                  }
                  const hasEvent = activeDays.includes(day);
                  const isSelected = selectedDay === day;

                  return (
                    <td
                      key={dIdx}
                      className={`calendar-day-cell ${hasEvent ? 'has-event' : ''} ${isSelected ? 'selected-day' : ''}`}
                      onClick={() => setSelectedDay(day)}
                    >
                      {day}
                    </td>
                  );
                })}
              </tr>
            ))}
          </tbody>
        </table>

        <div className="agenda-events-list">
          <div className="agenda-event-item">
            <span className="event-bullet">•</span> Atividade II
          </div>
          <div className="agenda-event-item">
            <span className="event-bullet">•</span> Início de: Atividade III
          </div>
          <div className="agenda-event-item">
            <span className="event-bullet">•</span> Prazo de Entrega: Questionário Módulo 1
          </div>
        </div>
      </div>
    </div>
  );
};
