import { useCallback, useEffect, useMemo, useState } from 'react';
import LoginCard from './components/LoginCard';
import Notification from './components/Notification';
import ReceiptTable from './components/ReceiptTable';
import UploadForm from './components/UploadForm';
import {
  ReceiptDto,
  bookReceipt,
  fetchReceipts,
  getApiBase,
  login,
  setAuthToken,
  uploadReceipt,
} from './api/client';

type Notice = {
  type: 'success' | 'error' | 'info';
  message: string;
};

const TOKEN_STORAGE_KEY = 'lexiflow_token';

const isUnauthorized = (error: unknown): boolean =>
  error instanceof Error && (error as Error & { status?: number }).status === 401;

const App = () => {
  const [token, setToken] = useState<string | null>(() => localStorage.getItem(TOKEN_STORAGE_KEY));
  const [receipts, setReceipts] = useState<ReceiptDto[]>([]);
  const [authLoading, setAuthLoading] = useState(false);
  const [authError, setAuthError] = useState<string | null>(null);
  const [loadingReceipts, setLoadingReceipts] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [bookingId, setBookingId] = useState<string | null>(null);
  const [notice, setNotice] = useState<Notice | null>(null);
  const apiBase = useMemo(() => getApiBase(), []);

  const showNotice = useCallback((type: Notice['type'], message: string) => {
    setNotice({ type, message });
  }, []);

  const loadReceipts = useCallback(async () => {
    setLoadingReceipts(true);
    try {
      const data = await fetchReceipts();
      data.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
      setReceipts(data);
    } catch (error) {
      if (isUnauthorized(error)) {
        setToken(null);
        showNotice('error', 'Session expired. Please sign in again.');
        return;
      }
      showNotice('error', error instanceof Error ? error.message : 'Failed to load receipts');
    } finally {
      setLoadingReceipts(false);
    }
  }, [showNotice]);

  useEffect(() => {
    if (!notice) {
      return;
    }
    const timer = window.setTimeout(() => setNotice(null), 4000);
    return () => window.clearTimeout(timer);
  }, [notice]);

  useEffect(() => {
    if (token) {
      setAuthToken(token);
      localStorage.setItem(TOKEN_STORAGE_KEY, token);
      void loadReceipts();
    } else {
      setAuthToken(null);
      localStorage.removeItem(TOKEN_STORAGE_KEY);
      setReceipts([]);
    }
  }, [token, loadReceipts]);

  const handleLogin = useCallback(
    async (username: string, password: string) => {
      setAuthLoading(true);
      setAuthError(null);
      try {
        const response = await login(username, password);
        setToken(response.token);
        showNotice('success', 'Signed in successfully. Receipts are loading.');
      } catch (error) {
        const message = error instanceof Error ? error.message : 'Unable to sign in';
        setAuthError(message);
        showNotice('error', message);
      } finally {
        setAuthLoading(false);
      }
    },
    [showNotice]
  );

  const handleLogout = () => {
    setToken(null);
    showNotice('info', 'Signed out.');
  };

  const handleUpload = useCallback(
    async (file: File) => {
      setUploading(true);
      try {
        const receipt = await uploadReceipt(file);
        setReceipts((prev) => [receipt, ...prev]);
        showNotice('success', `Uploaded receipt from ${receipt.vendor}.`);
      } catch (error) {
        if (isUnauthorized(error)) {
          setToken(null);
          showNotice('error', 'Session expired. Please sign in again.');
          return;
        }
        const message = error instanceof Error ? error.message : 'Upload failed';
        showNotice('error', message);
      } finally {
        setUploading(false);
      }
    },
    [showNotice]
  );

  const handleBook = useCallback(
    async (receipt: ReceiptDto) => {
      if (receipt.status.toLowerCase() === 'booked') {
        return;
      }
      setBookingId(receipt.id);
      try {
        const response = await bookReceipt(receipt.id);
        setReceipts((prev) =>
          prev.map((item) =>
            item.id === receipt.id
              ? {
                  ...item,
                  status: 'Booked',
                  voucherId: response.voucherId,
                }
              : item
          )
        );
        showNotice('success', `Receipt booked. Voucher ${response.voucherId}`);
      } catch (error) {
        if (isUnauthorized(error)) {
          setToken(null);
          showNotice('error', 'Session expired. Please sign in again.');
          return;
        }
        const message = error instanceof Error ? error.message : 'Booking failed';
        showNotice('error', message);
      } finally {
        setBookingId(null);
      }
    },
    [showNotice]
  );

  if (!token) {
    return (
      <main className="layout layout--centered">
        {notice ? <Notification {...notice} onClose={() => setNotice(null)} /> : null}
        <LoginCard onLogin={handleLogin} loading={authLoading} error={authError} />
        <section className="info-panel">
          <h2>What you can try</h2>
          <ul>
            <li>Sign in with the seeded demo user (demo / demo123!).</li>
            <li>Inspect the pre-seeded receipts after login.</li>
            <li>Upload new receipts to trigger OCR extraction.</li>
            <li>Book a receipt to send it to the LexOffice mock service.</li>
          </ul>
          <p className="info-panel__hint">API base: {apiBase}</p>
        </section>
      </main>
    );
  }

  return (
    <main className="layout">
      <header className="topbar">
        <div>
          <h1>LexiFlow Control Center</h1>
          <p>Manage seeded receipts, upload files, and run the booking workflow end-to-end.</p>
        </div>
        <div className="topbar__actions">
          <span className="badge badge--info">API: {apiBase}</span>
          <button type="button" className="button" onClick={handleLogout}>
            Sign out
          </button>
        </div>
      </header>

      {notice ? <Notification {...notice} onClose={() => setNotice(null)} /> : null}

      <section className="panel">
        <div className="panel__header">
          <h2>Receipts</h2>
          <button type="button" className="button button--ghost" onClick={() => loadReceipts()} disabled={loadingReceipts}>
            {loadingReceipts ? 'Refreshing…' : 'Refresh'}
          </button>
        </div>
        {loadingReceipts ? <p className="loading">Loading receipts…</p> : <ReceiptTable receipts={receipts} onBook={handleBook} bookingId={bookingId} />}
      </section>

      <section className="panel">
        <UploadForm onUpload={handleUpload} uploading={uploading} />
        <div className="panel__footer">
          <p>
            Need a sample? Download from the OCR container: <code>ocr/samples/r1.png</code> or <code>ocr/samples/r2.png</code>.
          </p>
        </div>
      </section>
    </main>
  );
};

export default App;
