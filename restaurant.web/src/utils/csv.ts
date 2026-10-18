export function csvCell(value: string | number): string {
  if (typeof value === 'number') {
    if (!Number.isFinite(value)) throw new Error('قيمة غير صالحة في التقرير.');
    return String(value);
  }
  // Quoting alone does not prevent spreadsheet formula evaluation.
  const text = /^[\s]*[=+\-@]/.test(value) || /^[\t\r\n]/.test(value) ? `'${value}` : value;
  return `"${text.replace(/"/g, '""')}"`;
}

export function createCsv(rows: (string | number)[][]): string {
  return '\uFEFF' + rows.map(row => row.map(csvCell).join(',')).join('\r\n');
}
