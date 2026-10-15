import React, { useState } from 'react';
import { usePosStore } from '../store/posStore';
import { Lock, User, AlertCircle, Eye, EyeOff } from 'lucide-react';

export const Login: React.FC = () => {
  const login = usePosStore((state) => state.login);
  const registerRestaurant = usePosStore((state) => state.registerRestaurant);
  const isLoading = usePosStore((state) => state.isLoading);
  
  // Sign In states
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Sign Up states
  const [isRegistering, setIsRegistering] = useState(false);
  const [step, setStep] = useState(1); // 1: Info, 2: Success
  const [regName, setRegName] = useState('');
  const [regTagline, setRegTagline] = useState('');
  const [regAddress, setRegAddress] = useState('');
  const [regPhone, setRegPhone] = useState('');
  const [regFullName, setRegFullName] = useState('');
  const [regUsername, setRegUsername] = useState('');
  const [regPassword, setRegPassword] = useState('');
  const handleSignIn = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    if (!username || !password) {
      setError('Please enter both username and password.');
      return;
    }

    const success = await login(username, password);
    if (!success) {
      setError('Invalid username or password. Please try again.');
    }
  };

  const handleRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    
    if (step === 1) {
      if (!regName || !regPhone || !regFullName || !regUsername || !regPassword) {
        setError('Please fill in all required fields.');
        return;
      }
      await executeRegister();
    }
  };

  const executeRegister = async () => {
    try {
      await registerRestaurant(
        regName,
        regTagline,
        regAddress || "HQ Address",
        regPhone,
        regFullName,
        regUsername,
        regPassword
      );
      setStep(2);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Registration failed. Username may already exist.');
    }
  };

  return (
    <div className="min-h-screen bg-slate-950 flex items-center justify-center p-4 relative overflow-hidden select-none">
      {/* Background ambient glows */}
      <div className="absolute top-1/4 left-1/4 w-96 h-96 bg-orange-600/10 rounded-full blur-3xl animate-pulse" />
      <div className="absolute bottom-1/4 right-1/4 w-96 h-96 bg-indigo-600/10 rounded-full blur-3xl" />

      <div className="w-full max-w-lg glass-panel glass-panel-glow p-8 rounded-3xl relative z-10">
        <div className="text-center mb-6 flex flex-col items-center">
          <img src="/logo.jpg" alt="GustoFlow Logo" className="w-14 h-14 rounded-2xl object-cover shadow-xl shadow-orange-600/20 mb-3 hover:scale-105 transition" />
          <h2 className="text-2xl font-extrabold text-white tracking-tight">GustoFlow</h2>
          <p className="text-slate-400 text-xs mt-1 font-medium">Smart Restaurant SaaS POS & Inventory</p>
        </div>

        {error && (
          <div className="flex items-center gap-2 bg-rose-500/10 border border-rose-500/20 text-rose-400 p-3.5 rounded-xl text-xs font-semibold mb-4">
            <AlertCircle className="w-4 h-4 shrink-0" />
            <span>{error}</span>
          </div>
        )}

        {!isRegistering ? (
          /* Sign In Form */
          <form onSubmit={handleSignIn} className="space-y-4">
            <div>
              <label className="block text-xs font-bold text-slate-400 mb-1.5 uppercase tracking-wider">Username</label>
              <div className="relative">
                <User className="w-4 h-4 text-slate-500 absolute left-4 top-1/2 -translate-y-1/2" />
                <input
                  type="text"
                  placeholder="e.g. superadmin"
                  value={username}
                  onChange={(e) => setUsername(e.target.value)}
                  className="w-full bg-slate-950/85 border border-slate-800 focus:border-orange-500 focus:ring-2 focus:ring-orange-500/20 rounded-xl pl-11 pr-4 py-3 text-sm text-white placeholder-slate-600 focus:outline-none transition-all duration-200"
                  required
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-400 mb-1.5 uppercase tracking-wider">Password</label>
              <div className="relative">
                <Lock className="w-4 h-4 text-slate-500 absolute left-4 top-1/2 -translate-y-1/2" />
                <input
                  type={showPassword ? 'text' : 'password'}
                  placeholder="••••••••"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className="w-full bg-slate-950/85 border border-slate-800 focus:border-orange-500 focus:ring-2 focus:ring-orange-500/20 rounded-xl pl-11 pr-11 py-3 text-sm text-white placeholder-slate-600 focus:outline-none transition-all duration-200"
                  required
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-4 top-1/2 -translate-y-1/2 text-slate-500 hover:text-white cursor-pointer transition-colors"
                >
                  {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                </button>
              </div>
            </div>

            <button
              type="submit"
              disabled={isLoading}
              className="w-full bg-gradient-to-r from-orange-500 to-orange-600 hover:from-orange-400 hover:to-orange-500 disabled:opacity-50 text-white py-3 rounded-xl font-bold text-sm transition shadow-lg shadow-orange-600/20 cursor-pointer btn-interactive"
            >
              {isLoading ? 'Signing In...' : 'Sign In'}
            </button>

            <div className="mt-8 border-t border-slate-800/80 pt-5 text-left">
              <p className="text-slate-500 text-[10px] font-bold uppercase tracking-wider text-center mb-2.5">Demo Accounts (Password: Password123!)</p>
              <div className="flex justify-center">
                <button
                  type="button"
                  onClick={() => {
                    setUsername('superadmin');
                    setPassword('Password123!');
                  }}
                  className="w-1/2 px-2 py-2.5 bg-slate-950/60 hover:bg-slate-900 border border-slate-800/80 hover:border-slate-700 text-[11px] font-semibold text-slate-300 rounded-xl transition cursor-pointer text-center btn-interactive"
                >
                  👑 Super Admin
                </button>
              </div>
            </div>

            <div className="mt-5 text-center">
              <button 
                type="button"
                onClick={() => { setIsRegistering(true); setStep(1); setError(null); }}
                className="text-xs text-orange-400 hover:text-orange-300 font-bold uppercase tracking-wider cursor-pointer transition-colors"
              >
                Create a New Restaurant Account
              </button>
            </div>
          </form>
        ) : (
          /* Multi-Step Sign Up Form */
          <form onSubmit={handleRegister} className="space-y-4">
            {step === 1 && (
              /* Step 1: Info */
              <div className="space-y-3">
                <div className="text-left mb-2">
                  <h3 className="text-sm font-bold text-white uppercase tracking-wider">Step 1: Restaurant Details</h3>
                </div>
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="block text-[10px] font-bold text-slate-400 mb-1">Restaurant Name *</label>
                    <input 
                      type="text" 
                      value={regName} 
                      onChange={(e) => setRegName(e.target.value)}
                      placeholder="e.g. Royal Dine" 
                      className="w-full bg-slate-950/80 border border-slate-800 focus:border-orange-500 focus:ring-2 focus:ring-orange-500/20 rounded-xl px-3 py-2.5 text-xs text-white placeholder-slate-600 focus:outline-none transition-all duration-200"
                      required
                    />
                  </div>
                  <div>
                    <label className="block text-[10px] font-bold text-slate-400 mb-1">Tagline</label>
                    <input 
                      type="text" 
                      value={regTagline} 
                      onChange={(e) => setRegTagline(e.target.value)}
                      placeholder="e.g. Fine Taste" 
                      className="w-full bg-slate-950/80 border border-slate-800 focus:border-orange-500 focus:ring-2 focus:ring-orange-500/20 rounded-xl px-3 py-2.5 text-xs text-white placeholder-slate-600 focus:outline-none transition-all duration-200"
                    />
                  </div>
                </div>
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="block text-[10px] font-bold text-slate-400 mb-1">Phone Number *</label>
                    <input 
                      type="tel" 
                      value={regPhone} 
                      onChange={(e) => setRegPhone(e.target.value)}
                      placeholder="e.g. +91 99999" 
                      className="w-full bg-slate-950/80 border border-slate-800 focus:border-orange-500 focus:ring-2 focus:ring-orange-500/20 rounded-xl px-3 py-2.5 text-xs text-white placeholder-slate-600 focus:outline-none transition-all duration-200"
                      required
                    />
                  </div>
                  <div>
                    <label className="block text-[10px] font-bold text-slate-400 mb-1">Address</label>
                    <input 
                      type="text" 
                      value={regAddress} 
                      onChange={(e) => setRegAddress(e.target.value)}
                      placeholder="e.g. MG Road, Delhi" 
                      className="w-full bg-slate-950/80 border border-slate-800 focus:border-orange-500 focus:ring-2 focus:ring-orange-500/20 rounded-xl px-3 py-2.5 text-xs text-white placeholder-slate-600 focus:outline-none transition-all duration-200"
                    />
                  </div>
                </div>

                <div className="border-t border-slate-800/80 my-4 pt-3 text-left">
                  <h3 className="text-sm font-bold text-white uppercase tracking-wider mb-2">Restaurant User Credentials</h3>
                </div>

                <div>
                  <label className="block text-[10px] font-bold text-slate-400 mb-1">Full Name *</label>
                  <input 
                    type="text" 
                    value={regFullName} 
                    onChange={(e) => setRegFullName(e.target.value)}
                    placeholder="e.g. John Doe" 
                    className="w-full bg-slate-950/80 border border-slate-800 focus:border-orange-500 focus:ring-2 focus:ring-orange-500/20 rounded-xl px-3 py-2.5 text-xs text-white placeholder-slate-600 focus:outline-none transition-all duration-200"
                    required
                  />
                </div>
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="block text-[10px] font-bold text-slate-400 mb-1">Restaurant Username *</label>
                    <input 
                      type="text" 
                      value={regUsername} 
                      onChange={(e) => setRegUsername(e.target.value)}
                      placeholder="Username for login" 
                      className="w-full bg-slate-950/80 border border-slate-800 focus:border-orange-500 focus:ring-2 focus:ring-orange-500/20 rounded-xl px-3 py-2.5 text-xs text-white placeholder-slate-600 focus:outline-none transition-all duration-200"
                      required
                    />
                  </div>
                  <div>
                    <label className="block text-[10px] font-bold text-slate-400 mb-1">Restaurant Password *</label>
                    <input 
                      type="password" 
                      value={regPassword} 
                      onChange={(e) => setRegPassword(e.target.value)}
                      placeholder="Password" 
                      className="w-full bg-slate-950/80 border border-slate-800 focus:border-orange-500 focus:ring-2 focus:ring-orange-500/20 rounded-xl px-3 py-2.5 text-xs text-white placeholder-slate-600 focus:outline-none transition-all duration-200"
                      required
                    />
                  </div>
                </div>
                <button
                  type="submit"
                  className="w-full bg-gradient-to-r from-orange-500 to-orange-600 hover:from-orange-400 hover:to-orange-500 text-white py-3 rounded-xl font-bold text-sm transition mt-3 cursor-pointer btn-interactive shadow-lg shadow-orange-600/10"
                >
                  Create Restaurant User
                </button>
              </div>
            )}

            {step === 2 && (
              /* Step 2: Success Screen */
              <div className="space-y-4 py-4 text-center">
                <div className="text-4xl animate-bounce">🎉</div>
                <h3 className="text-lg font-bold text-emerald-400 uppercase tracking-wide">Registration Successful!</h3>
                <p className="text-xs text-slate-300 max-w-sm mx-auto leading-relaxed">
                  Your GustoFlow restaurant account has been created successfully. Sign in with the restaurant user credentials you just created.
                </p>
                <button
                  type="button"
                  onClick={() => {
                    setIsRegistering(false);
                    setStep(1);
                    setError(null);
                  }}
                  className="w-full bg-gradient-to-r from-orange-500 to-orange-600 hover:from-orange-400 hover:to-orange-500 text-white py-3 rounded-xl font-bold text-sm transition mt-4 cursor-pointer btn-interactive shadow-lg shadow-orange-600/10"
                >
                  Proceed to Login
                </button>
              </div>
            )}

            {step !== 2 && (
              <div className="mt-6 text-center">
                <button 
                  type="button" 
                  onClick={() => { setIsRegistering(false); setStep(1); setError(null); }}
                  className="text-xs text-slate-500 hover:text-white font-bold uppercase tracking-wider cursor-pointer transition-colors"
                >
                  Already have an account? Sign In
                </button>
              </div>
            )}
          </form>
        )}
      </div>
    </div>
  );
};

export default Login;
