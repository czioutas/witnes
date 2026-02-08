import React, { useState } from 'react';

interface CodeExample {
  language: string;
  label: string;
  code: string;
}

interface CodeBlockProps {
  examples: CodeExample[];
}

const CodeBlock: React.FC<CodeBlockProps> = ({ examples }) => {
  const [activeTab, setActiveTab] = useState(0);

  if (examples.length === 1) {
    return (
      <div className="bg-slate-900 rounded-lg p-4 overflow-x-auto">
        <pre className="text-slate-300 text-sm">
          <code>{examples[0].code}</code>
        </pre>
      </div>
    );
  }

  return (
    <div className="bg-slate-900 rounded-lg overflow-hidden">
      <div className="flex border-b border-slate-700">
        {examples.map((example, index) => (
          <button
            key={index}
            onClick={() => setActiveTab(index)}
            className={`px-4 py-2 text-sm font-medium ${
              activeTab === index
                ? 'bg-slate-800 text-slate-200'
                : 'text-slate-400 hover:text-slate-300'
            }`}
          >
            {example.label}
          </button>
        ))}
      </div>
      <div className="p-4 overflow-x-auto">
        <pre className="text-slate-300 text-sm">
          <code>{examples[activeTab].code}</code>
        </pre>
      </div>
    </div>
  );
};

export default CodeBlock;