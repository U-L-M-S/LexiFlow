import { FormEvent, useState } from 'react';

interface LoginCardProps {
  onLogin: (username: string, password: string) => Promise<void> | void;
  loading: boolean;
  error?: string | null;
}

const LoginCard = ({ onLogin, loading, error }: LoginCardProps) => {
  const [username, setUsername] = useState('demo');
  const [password, setPassword] = useState('demo123!');

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    await onLogin(username.trim(), password);
  };

  return (
    <div className="card">
      <div className="card__header">
        <h1>LexiFlow Demo</h1>
        <p className="card__subtitle">Sign in with the seeded demo account to explore the receipt pipeline.</p>
      </div>
      <form className="card__form" onSubmit={handleSubmit}>
        <label className="form-field">
          <span>Username</span>
          <input
            type="text"
            value={username}
            onChange={(event) => setUsername(event.target.value)}
            placeholder="demo"
            autoComplete="username"
            required
          />
        </label>
        <label className="form-field">
          <span>Password</span>
          <input
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            placeholder="demo123!"
            autoComplete="current-password"
            required
          />
        </label>
        {error ? <p className="form-error">{error}</p> : <p className="form-hint">Hint: demo / demo123!</p>}
        <button type="submit" className="button button--primary" disabled={loading}>
          {loading ? 'Signing in…' : 'Sign in'}
        </button>
      </form>
    </div>
  );
};

export default LoginCard;
