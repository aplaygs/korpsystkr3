/**
 * @license
 * SPDX-License-Identifier: Apache-2.0
 */

import { FileCode, Copy, Check, MonitorSmartphone, AlertCircle } from 'lucide-react';
import { useState } from 'react';

// Загружаем все сгенерированные файлы кроссплатформенного проекта
const filesInfo = import.meta.glob('../AvaloniaHttpMonitor/**/*', { query: '?raw', import: 'default', eager: true }) as Record<string, string>;

export default function App() {
  const filePaths = Object.keys(filesInfo).sort();
  const [activeFile, setActiveFile] = useState(filePaths.find(p => p.includes('MainWindow.xaml.cs')) || filePaths[0] || '');
  const [copied, setCopied] = useState(false);

  const handleCopy = () => {
    if (activeFile && filesInfo[activeFile]) {
      navigator.clipboard.writeText(filesInfo[activeFile]);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  const getFileName = (path: string) => path.replace('../AvaloniaHttpMonitor/', '');

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col font-sans">
      {/* Header */}
      <div className="bg-slate-900 text-white p-6 shadow-md">
        <div className="max-w-6xl mx-auto">
          <h1 className="text-2xl font-bold mb-2 flex items-center">
            <MonitorSmartphone className="mr-3 h-6 w-6 text-blue-400" />
            Задание: Система Мониторинга (Кроссплатформенная)
          </h1>
          <div className="bg-blue-900/40 border border-blue-700/50 rounded-lg p-4 mt-4 flex items-start text-blue-100">
            <AlertCircle className="h-6 w-6 text-blue-400 mr-3 shrink-0 mt-0.5" />
            <div>
              <p className="font-semibold text-white mb-1">Обратите внимание: это веб-среда (браузер)</p>
              <p className="text-sm line-height-relaxed">
                WPF — это технология Microsoft, которая <strong>работает только на Windows</strong>! Именно поэтому на Mac вы получаете ошибку <code>NETSDK1100</code>. Чтобы вы все равно смогли запустить и сдать задание, я переписал проект на <strong>Avalonia UI</strong> (кроссплатформенный аналог WPF).
                <br className="mt-2 block"/>
                <strong>Как запустить на Mac:</strong> <br/>
                1. Скачайте проект целиком (шестеренка вверху справа ➡️ Export to ZIP)<br/>
                2. Раззипуйте, откройте терминал и зайдите в папку: <code>cd AvaloniaHttpMonitor</code><br/>
                3. Выполните команду: <code>dotnet run</code> (откроется нативное окно сервера и клиента!)
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Main Content: File Explorer */}
      <div className="flex-1 max-w-6xl w-full mx-auto flex p-6 gap-6 h-[calc(100vh-180px)]">
        
        {/* Sidebar */}
        <div className="w-1/3 bg-white rounded-xl shadow-sm border border-slate-200 overflow-hidden flex flex-col">
          <div className="bg-slate-100 border-b border-slate-200 px-4 py-3 font-semibold text-slate-700">
            Файлы проекта
          </div>
          <div className="overflow-y-auto flex-1 p-2">
            {filePaths.map(path => {
              const isActive = activeFile === path;
              const name = getFileName(path);
              const isFolder = false; // Simple flat list for now or we can style paths
              
              return (
                <button
                  key={path}
                  onClick={() => setActiveFile(path)}
                  className={`w-full text-left px-3 py-2 rounded-md mb-1 text-sm flex items-center transition-colors ${
                    isActive 
                      ? 'bg-blue-50 text-blue-700 font-medium' 
                      : 'text-slate-600 hover:bg-slate-50'
                  }`}
                >
                  <FileCode className={`h-4 w-4 mr-2 ${isActive ? 'text-blue-500' : 'text-slate-400'}`} />
                  <span className="truncate">{name}</span>
                </button>
              );
            })}
          </div>
        </div>

        {/* Editor View */}
        <div className="w-2/3 bg-[#1e1e1e] rounded-xl shadow-xl border border-slate-300 overflow-hidden flex flex-col">
          <div className="bg-[#2d2d2d] border-b border-black/20 px-4 py-3 flex justify-between items-center text-slate-300">
            <div className="font-mono text-sm">
              {activeFile ? getFileName(activeFile) : 'Выберите файл'}
            </div>
            
            <button 
              onClick={handleCopy}
              disabled={!activeFile}
              className="flex items-center text-xs bg-[#3d3d3d] hover:bg-[#4d4d4d] px-3 py-1.5 rounded transition-colors"
            >
              {copied ? (
                <><Check className="h-3 w-3 mr-1.5 text-green-400" /> Скопировано</>
              ) : (
                <><Copy className="h-3 w-3 mr-1.5" /> Копировать код</>
              )}
            </button>
          </div>
          
          <div className="flex-1 overflow-auto bg-[#1e1e1e] p-4">
            <pre className="font-mono text-[13px] leading-relaxed text-[#d4d4d4]">
              <code>
                {activeFile ? filesInfo[activeFile] : '// Нет выбранного файла'}
              </code>
            </pre>
          </div>
        </div>

      </div>
    </div>
  );
}
