import { ReceiptDto } from '../api/client';

interface ReceiptTableProps {
  receipts: ReceiptDto[];
  onBook: (receipt: ReceiptDto) => Promise<void>;
  bookingId?: string | null;
}

const formatCurrency = (value: number, currency = 'EUR') => {
  return new Intl.NumberFormat('de-DE', {
    style: 'currency',
    currency,
    minimumFractionDigits: 2,
  }).format(value);
};

const formatDate = (value: string) => {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }
  return new Intl.DateTimeFormat('de-DE', { dateStyle: 'medium' }).format(date);
};

const ReceiptTable = ({ receipts, onBook, bookingId }: ReceiptTableProps) => {
  if (!receipts.length) {
    return <p className="empty">No receipts yet. Upload a file to create one.</p>;
  }

  return (
    <div className="table-wrapper">
      <table className="table">
        <thead>
          <tr>
            <th>Vendor</th>
            <th>Date</th>
            <th>Total</th>
            <th>VAT</th>
            <th>Status</th>
            <th>Voucher</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {receipts.map((receipt) => {
            const isBooked = receipt.status.toLowerCase() === 'booked';
            const isProcessing = bookingId === receipt.id;
            return (
              <tr key={receipt.id}>
                <td>
                  <div className="table__vendor">
                    <strong>{receipt.vendor}</strong>
                    <small title={receipt.rawText || undefined}>{receipt.invoiceDate}</small>
                  </div>
                </td>
                <td>{formatDate(receipt.invoiceDate)}</td>
                <td>{formatCurrency(receipt.total, receipt.currency)}</td>
                <td>{formatCurrency(receipt.vat, receipt.currency)}</td>
                <td>
                  <span className={`badge badge--${isBooked ? 'success' : 'pending'}`}>{receipt.status}</span>
                </td>
                <td>{receipt.voucherId || '—'}</td>
                <td>
                  <button
                    type="button"
                    className="button button--ghost"
                    disabled={isBooked || isProcessing}
                    onClick={() => onBook(receipt)}
                  >
                    {isBooked ? 'Booked' : isProcessing ? 'Booking…' : 'Book'}
                  </button>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
};

export default ReceiptTable;
