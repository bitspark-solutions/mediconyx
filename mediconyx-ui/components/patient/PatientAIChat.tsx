"use client";
import { useState, useEffect, useRef } from "react";
import { apiClient } from "../../lib/apiClient";
import { Send, Bot, User, Calendar, FileText, Clock, AlertCircle, ChevronDown, ChevronUp, Loader2 } from "lucide-react";

interface AIMessage {
  id: number;
  conversationId: number;
  role: string;
  content: string;
  citations?: RagCitation[];
  confidence?: number;
  createdAt: string;
}

interface RagCitation {
  sourceType: string;
  sourceId: number;
  sourceName: string;
  excerpt: string;
  relevanceScore: number;
}

interface Conversation {
  id: number;
  patientId: number;
  title: string;
  createdAt: string;
  updatedAt: string;
  messageCount: number;
}

interface ChatResponse {
  answer: string;
  citations: RagCitation[];
  confidence: number;
  suggestedActions: string[];
  conversationId: number;
  messageId: number;
}

const SUGGESTED_QUESTIONS = [
  "What medications am I currently taking?",
  "Show me my recent lab results",
  "What is my blood pressure history?",
  "Summarize my health conditions",
  "When is my next appointment?",
];

export default function PatientAIChat() {
  const [conversations, setConversations] = useState<Conversation[]>([]);
  const [currentConversationId, setCurrentConversationId] = useState<number | null>(null);
  const [messages, setMessages] = useState<AIMessage[]>([]);
  const [input, setInput] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [showHistory, setShowHistory] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLTextAreaElement>(null);

  useEffect(() => {
    loadConversations();
  }, []);

  useEffect(() => {
    if (currentConversationId) {
      loadMessages(currentConversationId);
    }
  }, [currentConversationId]);

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  const loadConversations = async () => {
    try {
      const patientId = getCurrentPatientId();
      if (!patientId) return;

      const res = await apiClient.get(`/api/rag/patient/${patientId}/conversations`);
      if (res.ok) {
        const data = await res.json();
        setConversations(data.conversations || []);

        // Auto-select first conversation or create new
        if (data.conversations?.length > 0) {
          setCurrentConversationId(data.conversations[0].id);
        }
      }
    } catch (err) {
      console.error("Failed to load conversations:", err);
    }
  };

  const loadMessages = async (conversationId: number) => {
    try {
      const res = await apiClient.get(`/api/rag/conversation/${conversationId}/messages`);
      if (res.ok) {
        const data = await res.json();
        setMessages(data || []);
      }
    } catch (err) {
      console.error("Failed to load messages:", err);
    }
  };

  const getCurrentPatientId = (): number | null => {
    // In a real app, this would come from auth store
    // For now, we'll get it from the current user if they're a patient
    return null; // Will use first available conversation
  };

  const handleSend = async () => {
    if (!input.trim() || loading) return;

    const patientId = getCurrentPatientIdFromConversations();
    if (!patientId) {
      setError("Please select a patient first");
      return;
    }

    const userMessage = input.trim();
    setInput("");
    setLoading(true);
    setError("");

    try {
      // Add user message immediately (optimistic)
      const tempUserMsg: AIMessage = {
        id: Date.now(),
        conversationId: currentConversationId || 0,
        role: "User",
        content: userMessage,
        createdAt: new Date().toISOString(),
      };
      setMessages(prev => [...prev, tempUserMsg]);

      const res = await apiClient.post(`/api/rag/patient/${patientId}/chat`, {
        message: userMessage,
        conversationId: currentConversationId,
      });

      if (!res.ok) {
        const err = await res.json();
        throw new Error(err.message || "Failed to get response");
      }

      const data: ChatResponse = await res.json();

      // Add assistant response
      const assistantMsg: AIMessage = {
        id: data.messageId || Date.now() + 1,
        conversationId: data.conversationId,
        role: "Assistant",
        content: data.answer,
        citations: data.citations,
        confidence: data.confidence,
        createdAt: new Date().toISOString(),
      };
      setMessages(prev => [...prev, assistantMsg]);

      // Update conversation ID if new
      if (data.conversationId !== currentConversationId) {
        setCurrentConversationId(data.conversationId);
        loadConversations();
      }
    } catch (err: any) {
      setError(err.message || "Failed to send message");
      // Remove the optimistic user message on error
      setMessages(prev => prev.slice(0, -1));
    } finally {
      setLoading(false);
    }
  };

  const getCurrentPatientIdFromConversations = (): number | null => {
    if (currentConversationId && conversations.length > 0) {
      const conv = conversations.find(c => c.id === currentConversationId);
      return conv?.patientId || conversations[0]?.patientId || null;
    }
    return conversations[0]?.patientId || null;
  };

  const handleSuggestedQuestion = (question: string) => {
    setInput(question);
    inputRef.current?.focus();
  };

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  return (
    <div className="flex flex-col h-full bg-gray-900 rounded-xl border border-gray-800">
      {/* Header */}
      <div className="flex items-center justify-between p-4 border-b border-gray-800">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-full bg-purple-600/20 flex items-center justify-center">
            <Bot className="w-5 h-5 text-purple-400" />
          </div>
          <div>
            <h3 className="text-white font-medium">Health Assistant</h3>
            <p className="text-xs text-gray-500">AI-powered health insights</p>
          </div>
        </div>
        <button
          onClick={() => setShowHistory(!showHistory)}
          className="p-2 rounded-lg text-gray-400 hover:text-white hover:bg-gray-800 transition-colors"
        >
          {showHistory ? <ChevronUp className="w-5 h-5" /> : <ChevronDown className="w-5 h-5" />}
        </button>
      </div>

      {/* History Panel */}
      {showHistory && (
        <div className="border-b border-gray-800 p-4 max-h-48 overflow-y-auto">
          <h4 className="text-sm font-medium text-gray-400 mb-3">Previous Conversations</h4>
          {conversations.length === 0 ? (
            <p className="text-sm text-gray-500">No previous conversations</p>
          ) : (
            <div className="space-y-2">
              {conversations.map(conv => (
                <button
                  key={conv.id}
                  onClick={() => setCurrentConversationId(conv.id)}
                  className={`w-full text-left p-3 rounded-lg transition-colors ${
                    conv.id === currentConversationId
                      ? "bg-purple-600/20 border border-purple-500/30"
                      : "bg-gray-800/50 hover:bg-gray-800"
                  }`}
                >
                  <p className="text-sm text-white truncate">{conv.title}</p>
                  <p className="text-xs text-gray-500 mt-1">
                    {new Date(conv.updatedAt).toLocaleDateString()} • {conv.messageCount} messages
                  </p>
                </button>
              ))}
            </div>
          )}
        </div>
      )}

      {/* Messages */}
      <div className="flex-1 overflow-y-auto p-4 space-y-4">
        {messages.length === 0 && !loading && (
          <div className="flex flex-col items-center justify-center h-full text-center">
            <div className="w-16 h-16 rounded-full bg-purple-600/20 flex items-center justify-center mb-4">
              <Bot className="w-8 h-8 text-purple-400" />
            </div>
            <h3 className="text-lg font-medium text-white mb-2">Welcome to Health Assistant</h3>
            <p className="text-gray-400 text-sm max-w-md mb-6">
              Ask me questions about your health records, medications, lab results, and more.
              I can help you understand your health history.
            </p>

            {/* Suggested Questions */}
            <div className="flex flex-wrap gap-2 justify-center max-w-lg">
              {SUGGESTED_QUESTIONS.map((q, idx) => (
                <button
                  key={idx}
                  onClick={() => handleSuggestedQuestion(q)}
                  className="px-3 py-2 bg-gray-800 hover:bg-gray-700 text-gray-300 text-sm rounded-full border border-gray-700 hover:border-purple-500/30 transition-colors"
                >
                  {q}
                </button>
              ))}
            </div>
          </div>
        )}

        {messages.map((msg, idx) => (
          <div key={idx} className={`flex gap-3 ${msg.role === "User" ? "flex-row-reverse" : ""}`}>
            <div
              className={`w-8 h-8 rounded-full flex items-center justify-center flex-shrink-0 ${
                msg.role === "User"
                  ? "bg-blue-600/20 text-blue-400"
                  : "bg-purple-600/20 text-purple-400"
              }`}
            >
              {msg.role === "User" ? <User className="w-4 h-4" /> : <Bot className="w-4 h-4" />}
            </div>
            <div className={`flex-1 max-w-[80%] ${msg.role === "User" ? "text-right" : ""}`}>
              <div
                className={`inline-block p-4 rounded-2xl ${
                  msg.role === "User"
                    ? "bg-blue-600 text-white rounded-tr-sm"
                    : "bg-gray-800 text-gray-100 rounded-tl-sm"
                }`}
              >
                <p className="text-sm whitespace-pre-wrap">{msg.content}</p>
              </div>

              {/* Citations for assistant messages */}
              {msg.role === "Assistant" && msg.citations && msg.citations.length > 0 && (
                <div className="mt-2 space-y-2">
                  {msg.citations.map((cite, cIdx) => (
                    <div key={cIdx} className="flex items-start gap-2 text-xs text-gray-500 bg-gray-800/50 p-2 rounded-lg">
                      <FileText className="w-3 h-3 mt-0.5 flex-shrink-0" />
                      <div>
                        <p className="text-gray-400">{cite.sourceName}</p>
                        <p className="line-clamp-2">{cite.excerpt}</p>
                      </div>
                    </div>
                  ))}
                </div>
              )}

              <p className="text-xs text-gray-600 mt-1">
                {new Date(msg.createdAt).toLocaleTimeString()}
              </p>
            </div>
          </div>
        ))}

        {loading && (
          <div className="flex gap-3">
            <div className="w-8 h-8 rounded-full bg-purple-600/20 flex items-center justify-center">
              <Bot className="w-4 h-4 text-purple-400" />
            </div>
            <div className="bg-gray-800 p-4 rounded-2xl rounded-tl-sm">
              <div className="flex items-center gap-2 text-gray-400">
                <Loader2 className="w-4 h-4 animate-spin" />
                <span className="text-sm">Thinking...</span>
              </div>
            </div>
          </div>
        )}

        {error && (
          <div className="flex items-center gap-2 p-3 bg-red-500/10 border border-red-500/30 rounded-lg text-red-400 text-sm">
            <AlertCircle className="w-4 h-4 flex-shrink-0" />
            <p>{error}</p>
          </div>
        )}

        <div ref={messagesEndRef} />
      </div>

      {/* Input */}
      <div className="p-4 border-t border-gray-800">
        <div className="flex gap-2">
          <textarea
            ref={inputRef}
            value={input}
            onChange={e => setInput(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="Ask about your health..."
            className="flex-1 bg-gray-800 border border-gray-700 rounded-xl px-4 py-3 text-white placeholder-gray-500 focus:outline-none focus:border-purple-500 resize-none text-sm"
            rows={1}
          />
          <button
            onClick={handleSend}
            disabled={!input.trim() || loading}
            className="px-4 py-3 bg-purple-600 hover:bg-purple-500 disabled:bg-gray-700 disabled:text-gray-500 text-white rounded-xl transition-colors"
          >
            <Send className="w-4 h-4" />
          </button>
        </div>
        <p className="text-xs text-gray-600 mt-2 text-center">
          AI responses are for informational purposes only. Always consult your doctor for medical advice.
        </p>
      </div>
    </div>
  );
}
