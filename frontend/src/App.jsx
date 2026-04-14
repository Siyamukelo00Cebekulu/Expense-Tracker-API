import { useEffect, useMemo, useState } from 'react';

const API_URL = 'http://localhost:5083/api';
const categories = ['Groceries', 'Leisure', 'Electronics', 'Utilities', 'Clothing', 'Health', 'Others'];

function App() {
  const [mode, setMode] = useState('login');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [username, setUsername] = useState('');
  const [token, setToken] = useState(localStorage.getItem('token') || '');
  const [userInfo, setUserInfo] = useState({ username: '', email: '' });
  const [expenses, setExpenses] = useState([]);
  const [filter, setFilter] = useState('past-month');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [activeExpense, setActiveExpense] = useState(null);
  const [title, setTitle] = useState('');
  const [amount, setAmount] = useState('');
  const [category, setCategory] = useState(categories[0]);
  const [date, setDate] = useState(new Date().toISOString().slice(0, 10));
  const [notes, setNotes] = useState('');
  const [message, setMessage] = useState('');

  const authHeaders = useMemo(() => ({ Authorization: `Bearer ${token}` }), [token]);

  useEffect(() => {
    if (token) {
      loadExpenses();
      setUserInfo({ username: localStorage.getItem('username') || '', email: localStorage.getItem('email') || '' });
    }
  }, [token]);

  async function request(path, options = {}) {
    const headers = { ...options.headers };
    if (options.body) {
      headers['Content-Type'] = 'application/json';
    }

    const response = await fetch(`${API_URL}${path}`, {
      ...options,
      headers,
    });
    if (!response.ok) {
      const payload = await response.json().catch(() => ({}));
      throw new Error(payload.message || response.statusText);
    }
    return response.json().catch(() => null);
  }

  async function handleLogin() {
    try {
      const payload = await request('/auth/login', {
        method: 'POST',
        body: JSON.stringify({ email, password }),
      });
      setToken(payload.token);
      localStorage.setItem('token', payload.token);
      localStorage.setItem('username', payload.username);
      localStorage.setItem('email', payload.email);
      setUserInfo({ username: payload.username, email: payload.email });
      setMessage('Logged in successfully.');
    } catch (error) {
      setMessage(error.message);
    }
  }

  async function handleSignup() {
    try {
      await request('/auth/signup', {
        method: 'POST',
        body: JSON.stringify({ username, email, password }),
      });
      setMessage('Account created. Please log in.');
      setMode('login');
    } catch (error) {
      setMessage(error.message);
    }
  }

  async function loadExpenses() {
    try {
      let query = `?filter=${filter}`;
      if (filter === 'custom') {
        if (startDate) query += `&startDate=${startDate}`;
        if (endDate) query += `&endDate=${endDate}`;
      }
      const payload = await request(`/expenses${query}`, {
        method: 'GET',
        headers: authHeaders,
      });
      setExpenses(payload);
    } catch (error) {
      setMessage(error.message);
    }
  }

  async function saveExpense(event) {
    event.preventDefault();
    try {
      const body = { title, amount: parseFloat(amount), category, date, notes };
      if (activeExpense) {
        await request(`/expenses/${activeExpense.id}`, {
          method: 'PUT',
          headers: authHeaders,
          body: JSON.stringify(body),
        });
        setMessage('Expense updated.');
      } else {
        await request('/expenses', {
          method: 'POST',
          headers: authHeaders,
          body: JSON.stringify(body),
        });
        setMessage('Expense added.');
      }
      resetExpenseForm();
      loadExpenses();
    } catch (error) {
      setMessage(error.message);
    }
  }

  function resetExpenseForm() {
    setActiveExpense(null);
    setTitle('');
    setAmount('');
    setCategory(categories[0]);
    setDate(new Date().toISOString().slice(0, 10));
    setNotes('');
  }

  function editExpense(expense) {
    setActiveExpense(expense);
    setTitle(expense.title);
    setAmount(expense.amount.toString());
    setCategory(expense.category);
    setDate(expense.date.slice(0, 10));
    setNotes(expense.notes || '');
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  async function removeExpense(id) {
    try {
      await request(`/expenses/${id}`, {
        method: 'DELETE',
        headers: authHeaders,
      });
      setMessage('Expense deleted.');
      loadExpenses();
    } catch (error) {
      setMessage(error.message);
    }
  }

  function logout() {
    setToken('');
    localStorage.removeItem('token');
    localStorage.removeItem('username');
    localStorage.removeItem('email');
    setExpenses([]);
    setUserInfo({ username: '', email: '' });
  }

  return (
    <div className="app-container">
      <header>
        <h1>Expense Tracker</h1>
      </header>
      {message && <div className="message">{message}</div>}
      {!token ? (
        <div className="auth-panel">
          <div className="tabs">
            <button className={mode === 'login' ? 'active' : ''} onClick={() => setMode('login')}>
              Login
            </button>
            <button className={mode === 'signup' ? 'active' : ''} onClick={() => setMode('signup')}>
              Sign Up
            </button>
          </div>
          <div className="form-card">
            {mode === 'signup' && (
              <label>
                Username
                <input value={username} onChange={(e) => setUsername(e.target.value)} />
              </label>
            )}
            <label>
              Email
              <input value={email} onChange={(e) => setEmail(e.target.value)} />
            </label>
            <label>
              Password
              <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} />
            </label>
            <button onClick={mode === 'login' ? handleLogin : handleSignup}>
              {mode === 'login' ? 'Log In' : 'Create Account'}
            </button>
          </div>
        </div>
      ) : (
        <main>
          <section className="profile-bar">
            <div>
              <strong>{userInfo.username}</strong>
              <span>{userInfo.email}</span>
            </div>
            <button className="small" onClick={logout}>
              Logout
            </button>
          </section>
          <section className="expense-form-panel">
            <h2>{activeExpense ? 'Edit Expense' : 'Add New Expense'}</h2>
            <form onSubmit={saveExpense} className="expense-form">
              <label>
                Title
                <input value={title} onChange={(e) => setTitle(e.target.value)} required />
              </label>
              <label>
                Amount
                <input type="number" step="0.01" value={amount} onChange={(e) => setAmount(e.target.value)} required />
              </label>
              <label>
                Category
                <select value={category} onChange={(e) => setCategory(e.target.value)}>
                  {categories.map((item) => (
                    <option key={item} value={item}>
                      {item}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                Date
                <input type="date" value={date} onChange={(e) => setDate(e.target.value)} required />
              </label>
              <label>
                Notes
                <textarea value={notes} onChange={(e) => setNotes(e.target.value)} />
              </label>
              <div className="form-actions">
                <button type="submit">{activeExpense ? 'Update Expense' : 'Add Expense'}</button>
                {activeExpense && (
                  <button type="button" className="secondary" onClick={resetExpenseForm}>
                    Clear
                  </button>
                )}
              </div>
            </form>
          </section>
          <section className="expense-list-panel">
            <div className="filter-row">
              <div>
                <label>
                  Filter
                  <select value={filter} onChange={(e) => setFilter(e.target.value)}>
                    <option value="past-week">Past Week</option>
                    <option value="past-month">Past Month</option>
                    <option value="past-3-months">Last 3 Months</option>
                    <option value="custom">Custom</option>
                  </select>
                </label>
                {filter === 'custom' && (
                  <div className="custom-range">
                    <label>
                      From
                      <input type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} />
                    </label>
                    <label>
                      To
                      <input type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} />
                    </label>
                  </div>
                )}
              </div>
              <button className="small" onClick={loadExpenses}>
                Refresh
              </button>
            </div>
            <table>
              <thead>
                <tr>
                  <th>Title</th>
                  <th>Amount</th>
                  <th>Category</th>
                  <th>Date</th>
                  <th>Notes</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {expenses.length === 0 ? (
                  <tr>
                    <td colSpan="6">No expenses found.</td>
                  </tr>
                ) : (
                  expenses.map((expense) => (
                    <tr key={expense.id}>
                      <td>{expense.title}</td>
                      <td>${expense.amount.toFixed(2)}</td>
                      <td>{expense.category}</td>
                      <td>{new Date(expense.date).toLocaleDateString()}</td>
                      <td>{expense.notes || '-'}</td>
                      <td>
                        <button className="small" onClick={() => editExpense(expense)}>
                          Edit
                        </button>
                        <button className="small secondary" onClick={() => removeExpense(expense.id)}>
                          Delete
                        </button>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </section>
        </main>
      )}
    </div>
  );
}

export default App;
