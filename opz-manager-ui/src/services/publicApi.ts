import axios from 'axios';

const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:5000/api';

const publicApi = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add session ID to all public requests
publicApi.interceptors.request.use((config) => {
  const sessionId = localStorage.getItem('anonymousSessionId');
  if (sessionId) {
    config.headers['X-Session-Id'] = sessionId;
  }
  return config;
});

// ─── Types ───────────────────────────────────────────────

export interface PublicOPZDocument {
  id: number;
  filename: string;
  uploadDate: string;
  analysisStatus: string;
  requirementsCount: number;
  matchesCount: number;
}

export interface SectionCheck {
  name: string;
  found: boolean;
  details?: string;
}

export interface CompletenessResult {
  score: number;
  sections: SectionCheck[];
}

export interface NormCheck {
  name: string;
  referenced: boolean;
  details?: string;
}

export interface ComplianceResult {
  score: number;
  norms: NormCheck[];
}

export interface TechnicalResult {
  score: number;
  measurableParams: number;
  totalParams: number;
  qualifiersUsed: number;
  issues: string[];
}

export interface GapAnalysisResult {
  score: number;
  missingSections: string[];
  recommendations: string[];
}

export interface VerificationResult {
  id: number;
  opzDocumentId: number;
  overallScore: number;
  grade: string;
  completeness?: CompletenessResult;
  compliance?: ComplianceResult;
  technical?: TechnicalResult;
  gapAnalysis?: GapAnalysisResult;
  summaryText?: string;
  createdAt: string;
}

export interface LeadCaptureRequest {
  email: string;
  marketingConsent: boolean;
  opzDocumentId?: number;
  source: string;
}

export interface LeadCaptureResponse {
  downloadToken: string;
  expiresAt: string;
  message: string;
}

export interface DownloadRequest {
  downloadToken: string;
  content: string;
  title: string;
}

export interface EquipmentMatch {
  id: number;
  modelId: number;
  modelName: string;
  manufacturerName: string;
  typeName: string;
  matchScore: number;
  complianceDescription: string;
  createdAt: string;
}

export interface Manufacturer {
  id: number;
  name: string;
  description: string;
  createdAt: string;
}

export interface EquipmentType {
  id: number;
  name: string;
  description: string;
  createdAt: string;
}

export interface EquipmentModel {
  id: number;
  manufacturerId: number;
  manufacturerName: string;
  typeId: number;
  typeName: string;
  modelName: string;
  specificationsJson: string;
  createdAt: string;
}

// ─── Public OPZ API ─────────────────────────────────────

export const publicOPZAPI = {
  uploadOPZ: async (file: File): Promise<PublicOPZDocument> => {
    const formData = new FormData();
    formData.append('file', file);
    const response = await publicApi.post('/public/opz/upload', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return response.data;
  },

  getOPZDocument: async (id: number): Promise<PublicOPZDocument> => {
    const response = await publicApi.get(`/public/opz/${id}`);
    return response.data;
  },

  verifyOPZ: async (id: number): Promise<VerificationResult> => {
    const response = await publicApi.post(`/public/opz/${id}/verify`);
    return response.data;
  },

  getVerification: async (id: number): Promise<VerificationResult> => {
    const response = await publicApi.get(`/public/opz/${id}/verification`);
    return response.data;
  },

  analyzeOPZ: async (id: number): Promise<{ message: string; matchesCount: number; matches: EquipmentMatch[] }> => {
    const response = await publicApi.post(`/public/opz/${id}/analyze`);
    return response.data;
  },
};

// ─── Public Generator API ───────────────────────────────

export const publicGeneratorAPI = {
  generateContent: async (equipmentModelIds: number[], equipmentType: string): Promise<{ content: string }> => {
    const response = await publicApi.post('/public/generate/content', { equipmentModelIds, equipmentType });
    return response.data;
  },
};

// ─── Public Equipment API ───────────────────────────────

export const publicEquipmentAPI = {
  getTypes: async (): Promise<EquipmentType[]> => {
    const response = await publicApi.get('/public/equipment/types');
    return response.data;
  },
  getModels: async (): Promise<EquipmentModel[]> => {
    const response = await publicApi.get('/public/equipment/models');
    return response.data;
  },
  getManufacturers: async (): Promise<Manufacturer[]> => {
    const response = await publicApi.get('/public/equipment/manufacturers');
    return response.data;
  },
};

// ─── Lead Capture API ───────────────────────────────────

export const leadCaptureAPI = {
  captureLead: async (data: LeadCaptureRequest): Promise<LeadCaptureResponse> => {
    const response = await publicApi.post('/public/lead-capture', data);
    return response.data;
  },

  downloadPdf: async (data: DownloadRequest): Promise<Blob> => {
    const response = await publicApi.post('/public/download/pdf', data, {
      responseType: 'blob',
    });
    return response.data;
  },
};

export default publicApi;
