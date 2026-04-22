"use client";
import { useState, useCallback } from "react";
import { apiClient } from "../../lib/apiClient";
import {
  Upload,
  File,
  FileText,
  Image,
  X,
  CheckCircle2,
  AlertCircle,
  Loader2,
  Trash2,
  Download,
} from "lucide-react";

interface FileAttachment {
  id: number;
  storageFileId: string;
  patientId: number;
  patientName: string;
  fileName: string;
  originalFileName: string;
  contentType: string;
  fileSize: number;
  category: string;
  description?: string;
  isActive: boolean;
  uploadedAt: string;
  uploadedByUserName: string;
}

interface UploadedFile {
  file: File;
  preview?: string;
  progress: number;
  status: "pending" | "uploading" | "success" | "error";
  error?: string;
  attachmentId?: number;
}

const CATEGORIES = [
  { value: "0", label: "Lab Report", icon: FileText },
  { value: "1", label: "Imaging (X-Ray, MRI, CT)", icon: Image },
  { value: "2", label: "Prescription", icon: FileText },
  { value: "3", label: "Discharge Summary", icon: FileText },
  { value: "4", label: "Other", icon: File },
];

const ALLOWED_TYPES = [
  "application/pdf",
  "image/jpeg",
  "image/png",
  "image/gif",
];

const MAX_FILE_SIZE = 50 * 1024 * 1024; // 50MB

export default function PatientReportUpload() {
  const [files, setFiles] = useState<UploadedFile[]>([]);
  const [category, setCategory] = useState("0");
  const [description, setDescription] = useState("");
  const [patientId, setPatientId] = useState<number>(1); // Would come from auth
  const [attachments, setAttachments] = useState<FileAttachment[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [dragActive, setDragActive] = useState(false);

  // Load existing attachments
  useState(() => {
    loadAttachments();
  });

  const loadAttachments = async () => {
    try {
      const res = await apiClient.get(`/api/file-attachment/patient/${patientId}?pageSize=50`);
      if (res.ok) {
        const data = await res.json();
        setAttachments(data.attachments || []);
      }
    } catch (err) {
      console.error("Failed to load attachments:", err);
    }
  };

  const handleDrag = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.type === "dragenter" || e.type === "dragover") {
      setDragActive(true);
    } else if (e.type === "dragleave") {
      setDragActive(false);
    }
  }, []);

  const validateFile = (file: File): string | null => {
    if (!ALLOWED_TYPES.some(type => file.type.includes(type.split("/")[1]))) {
      return "File type not allowed. Please upload PDF, JPEG, PNG, or GIF.";
    }
    if (file.size > MAX_FILE_SIZE) {
      return "File size exceeds 50MB limit.";
    }
    return null;
  };

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);

    const droppedFiles = Array.from(e.dataTransfer.files);
    addFiles(droppedFiles);
  }, []);

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files) {
      const selectedFiles = Array.from(e.target.files);
      addFiles(selectedFiles);
    }
  };

  const addFiles = (newFiles: File[]) => {
    const validatedFiles = newFiles.map(file => {
      const error = validateFile(file);
      return {
        file,
        preview: file.type.startsWith("image/") ? URL.createObjectURL(file) : undefined,
        progress: 0,
        status: error ? ("error" as const) : ("pending" as const),
        error: error || undefined,
      };
    });

    setFiles(prev => [...prev, ...validatedFiles]);
  };

  const removeFile = (index: number) => {
    setFiles(prev => {
      const file = prev[index];
      if (file.preview) {
        URL.revokeObjectURL(file.preview);
      }
      return prev.filter((_, i) => i !== index);
    });
  };

  const uploadFiles = async () => {
    setLoading(true);
    setError("");

    const pendingFiles = files.filter(f => f.status === "pending");
    if (pendingFiles.length === 0) {
      setLoading(false);
      return;
    }

    for (let i = 0; i < pendingFiles.length; i++) {
      const fileData = pendingFiles[i];
      const fileIndex = files.findIndex(f => f.file === fileData.file);

      if (fileIndex === -1) continue;

      // Update status to uploading
      setFiles(prev => prev.map((f, idx) =>
        idx === fileIndex ? { ...f, status: "uploading" as const, progress: 0 } : f
      ));

      try {
        const formData = new FormData();
        formData.append("file", fileData.file);
        formData.append("patientId", patientId.toString());
        formData.append("category", category);
        if (description) formData.append("description", description);

        const res = await fetch("http://localhost:9765/api/fileattachment/upload", {
          method: "POST",
          headers: {
            Authorization: `Bearer ${localStorage.getItem("accessToken")}`,
          },
          body: formData,
        });

        if (!res.ok) {
          const err = await res.json();
          throw new Error(err.message || "Upload failed");
        }

        const attachment = await res.json();

        // Update status to success
        setFiles(prev => prev.map((f, idx) =>
          idx === fileIndex ? { ...f, status: "success" as const, progress: 100, attachmentId: attachment.id } : f
        ));

        // Reload attachments list
        loadAttachments();
      } catch (err: any) {
        setFiles(prev => prev.map((f, idx) =>
          idx === fileIndex ? { ...f, status: "error" as const, error: err.message } : f
        ));
      }
    }

    setLoading(false);
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  };

  const getFileIcon = (contentType: string) => {
    if (contentType.includes("pdf")) return <FileText className="w-5 h-5 text-red-400" />;
    if (contentType.includes("image")) return <Image className="w-5 h-5 text-blue-400" />;
    return <File className="w-5 h-5 text-gray-400" />;
  };

  return (
    <div className="space-y-6">
      {/* Upload Zone */}
      <div
        className={`relative border-2 border-dashed rounded-xl p-8 text-center transition-colors ${
          dragActive
            ? "border-purple-500 bg-purple-500/5"
            : "border-gray-700 hover:border-gray-600"
        }`}
        onDragEnter={handleDrag}
        onDragLeave={handleDrag}
        onDragOver={handleDrag}
        onDrop={handleDrop}
      >
        <input
          type="file"
          multiple
          accept=".pdf,.jpg,.jpeg,.png,.gif"
          onChange={handleFileSelect}
          className="absolute inset-0 w-full h-full opacity-0 cursor-pointer"
        />
        <Upload className="w-12 h-12 text-gray-500 mx-auto mb-4" />
        <h3 className="text-lg font-medium text-white mb-2">
          Drop your medical reports here
        </h3>
        <p className="text-sm text-gray-500 mb-4">
          or click to browse. Supports PDF, JPEG, PNG, GIF up to 50MB
        </p>
        <div className="flex flex-wrap gap-2 justify-center">
          {CATEGORIES.map(cat => (
            <span key={cat.value} className="px-2 py-1 bg-gray-800 text-gray-400 text-xs rounded-full">
              {cat.label}
            </span>
          ))}
        </div>
      </div>

      {/* File List */}
      {files.length > 0 && (
        <div className="bg-gray-900 border border-gray-800 rounded-xl p-4 space-y-3">
          <h4 className="text-sm font-medium text-gray-400">Files to Upload</h4>
          {files.map((fileData, index) => (
            <div
              key={index}
              className={`flex items-center gap-3 p-3 rounded-lg ${
                fileData.status === "error"
                  ? "bg-red-500/10 border border-red-500/30"
                  : fileData.status === "success"
                  ? "bg-green-500/10 border border-green-500/30"
                  : "bg-gray-800/50"
              }`}
            >
              {fileData.preview ? (
                <img
                  src={fileData.preview}
                  alt=""
                  className="w-12 h-12 rounded-lg object-cover"
                />
              ) : (
                <div className="w-12 h-12 rounded-lg bg-gray-700 flex items-center justify-center">
                  {getFileIcon(fileData.file.type)}
                </div>
              )}
              <div className="flex-1 min-w-0">
                <p className="text-sm text-white truncate">{fileData.file.name}</p>
                <p className="text-xs text-gray-500">
                  {formatFileSize(fileData.file.size)}
                  {fileData.status === "uploading" && " • Uploading..."}
                  {fileData.status === "success" && " • Uploaded"}
                  {fileData.status === "error" && ` • ${fileData.error}`}
                </p>
              </div>
              {fileData.status === "uploading" && (
                <Loader2 className="w-5 h-5 text-purple-400 animate-spin" />
              )}
              {fileData.status === "success" && (
                <CheckCircle2 className="w-5 h-5 text-green-400" />
              )}
              {fileData.status === "error" && (
                <AlertCircle className="w-5 h-5 text-red-400" />
              )}
              <button
                onClick={() => removeFile(index)}
                className="p-1 text-gray-400 hover:text-red-400 transition-colors"
              >
                <X className="w-4 h-4" />
              </button>
            </div>
          ))}

          {/* Category & Description */}
          <div className="pt-3 border-t border-gray-800 space-y-3">
            <div>
              <label className="block text-xs font-medium text-gray-400 mb-1.5">Category</label>
              <select
                value={category}
                onChange={e => setCategory(e.target.value)}
                className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500"
              >
                {CATEGORIES.map(cat => (
                  <option key={cat.value} value={cat.value}>{cat.label}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-400 mb-1.5">Description (optional)</label>
              <textarea
                value={description}
                onChange={e => setDescription(e.target.value)}
                placeholder="e.g., Annual blood test results"
                className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm focus:outline-none focus:border-purple-500 resize-none"
                rows={2}
              />
            </div>
          </div>

          {/* Upload Button */}
          <button
            onClick={uploadFiles}
            disabled={loading || !files.some(f => f.status === "pending")}
            className="w-full py-3 bg-purple-600 hover:bg-purple-500 disabled:bg-gray-700 disabled:text-gray-500 text-white rounded-xl font-medium transition-colors flex items-center justify-center gap-2"
          >
            {loading ? (
              <>
                <Loader2 className="w-4 h-4 animate-spin" />
                Uploading...
              </>
            ) : (
              <>
                <Upload className="w-4 h-4" />
                Upload {files.filter(f => f.status === "pending").length} file(s)
              </>
            )}
          </button>
        </div>
      )}

      {/* Existing Attachments */}
      {attachments.length > 0 && (
        <div className="space-y-3">
          <h4 className="text-sm font-medium text-gray-400">Your Uploaded Reports</h4>
          <div className="grid gap-3">
            {attachments.map(att => (
              <div
                key={att.id}
                className="flex items-center gap-3 p-4 bg-gray-900 border border-gray-800 rounded-xl hover:border-gray-700 transition-colors"
              >
                <div className="w-12 h-12 rounded-lg bg-gray-800 flex items-center justify-center">
                  {getFileIcon(att.contentType)}
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm text-white font-medium truncate">{att.originalFileName}</p>
                  <p className="text-xs text-gray-500">
                    {CATEGORIES.find(c => c.value === att.category)?.label || "Other"} •{" "}
                    {formatFileSize(att.fileSize)} •{" "}
                    {new Date(att.uploadedAt).toLocaleDateString()}
                  </p>
                  {att.description && (
                    <p className="text-xs text-gray-400 mt-1 truncate">{att.description}</p>
                  )}
                </div>
                <div className="flex items-center gap-1">
                  <a
                    href={`http://localhost:9765/api/fileattachment/${att.id}/download`}
                    className="p-2 text-gray-400 hover:text-white bg-gray-800 rounded-lg transition-colors"
                    title="Download"
                  >
                    <Download className="w-4 h-4" />
                  </a>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {error && (
        <div className="flex items-center gap-2 p-3 bg-red-500/10 border border-red-500/30 rounded-lg text-red-400 text-sm">
          <AlertCircle className="w-4 h-4 flex-shrink-0" />
          <p>{error}</p>
        </div>
      )}
    </div>
  );
}
