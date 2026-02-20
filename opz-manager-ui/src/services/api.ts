import axios from 'axios';

const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:5000/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add token to requests if available
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Handle token expiration - only redirect on admin routes
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      const isAdminRoute = window.location.pathname.startsWith('/admin') || window.location.pathname === '/login';
      if (isAdminRoute) {
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        window.location.href = '/login';
      }
    }
    return Promise.reject(error);
  }
);

// ─── Types ───────────────────────────────────────────────

export interface User {
  id: number;
  username: string;
  email: string;
  role: string;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  role?: string;
}

export interface LoginResponse {
  token: string;
  user: User;
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

export interface OPZDocument {
  id: number;
  filename: string;
  uploadDate: string;
  analysisStatus: string;
  requirementsCount: number;
  matchesCount: number;
}

export interface OPZRequirement {
  id: number;
  requirementText: string;
  requirementType: string;
  extractedSpecsJson: string;
  createdAt: string;
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

export interface OPZDocumentDetail {
  id: number;
  filename: string;
  uploadDate: string;
  analysisStatus: string;
  requirements: OPZRequirement[];
  matches: EquipmentMatch[];
}

export interface TrainingData {
  id: number;
  question: string;
  answer: string;
  context: string;
  dataType: string;
  createdAt: string;
}

export interface ConfigStatus {
  llmConnected: boolean;
  llmBaseUrl: string;
  manufacturersCount: number;
  equipmentTypesCount: number;
  equipmentModelsCount: number;
  opzDocumentsCount: number;
  trainingDataCount: number;
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

// ─── Auth API ────────────────────────────────────────────

export const authAPI = {
  login: async (credentials: LoginRequest): Promise<LoginResponse> => {
    const response = await api.post('/auth/login', credentials);
    return response.data;
  },
  register: async (userData: RegisterRequest): Promise<{ message: string; user: User }> => {
    const response = await api.post('/auth/register', userData);
    return response.data;
  },
  logout: async (): Promise<void> => {
    await api.post('/auth/logout');
  },
  test: async (): Promise<{ message: string; timestamp: string }> => {
    const response = await api.get('/auth/test');
    return response.data;
  },
};

// ─── Equipment API ───────────────────────────────────────

export const equipmentAPI = {
  getManufacturers: async (): Promise<Manufacturer[]> => {
    const response = await api.get('/equipment/manufacturers');
    return response.data;
  },
  getTypes: async (): Promise<EquipmentType[]> => {
    const response = await api.get('/equipment/types');
    return response.data;
  },
  getModels: async (): Promise<EquipmentModel[]> => {
    const response = await api.get('/equipment/models');
    return response.data;
  },
  getModel: async (id: number): Promise<EquipmentModel> => {
    const response = await api.get(`/equipment/models/${id}`);
    return response.data;
  },
  createManufacturer: async (data: { name: string; description: string }): Promise<Manufacturer> => {
    const response = await api.post('/equipment/manufacturers', data);
    return response.data;
  },
  createType: async (data: { name: string; description: string }): Promise<EquipmentType> => {
    const response = await api.post('/equipment/types', data);
    return response.data;
  },
  createModel: async (data: { manufacturerId: number; typeId: number; modelName: string; specificationsJson: string }): Promise<EquipmentModel> => {
    const response = await api.post('/equipment/models', data);
    return response.data;
  },
  deleteManufacturer: async (id: number): Promise<void> => {
    await api.delete(`/equipment/manufacturers/${id}`);
  },
  deleteType: async (id: number): Promise<void> => {
    await api.delete(`/equipment/types/${id}`);
  },
  deleteModel: async (id: number): Promise<void> => {
    await api.delete(`/equipment/models/${id}`);
  },
};

// ─── OPZ API ─────────────────────────────────────────────

export const opzAPI = {
  uploadOPZ: async (file: File): Promise<OPZDocument> => {
    const formData = new FormData();
    formData.append('file', file);
    const response = await api.post('/opz/upload', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return response.data;
  },
  getOPZDocuments: async (): Promise<OPZDocument[]> => {
    const response = await api.get('/opz');
    return response.data;
  },
  getOPZDocument: async (id: number): Promise<OPZDocumentDetail> => {
    const response = await api.get(`/opz/${id}`);
    return response.data;
  },
  analyzeOPZ: async (id: number): Promise<{ message: string; matchesCount: number; matches: EquipmentMatch[] }> => {
    const response = await api.post(`/opz/${id}/analyze`);
    return response.data;
  },
  getOPZMatches: async (id: number): Promise<EquipmentMatch[]> => {
    const response = await api.get(`/opz/${id}/matches`);
    return response.data;
  },
  deleteOPZ: async (id: number): Promise<void> => {
    await api.delete(`/opz/${id}`);
  },
};

// ─── Generator API ───────────────────────────────────────

export const generatorAPI = {
  generateContent: async (equipmentModelIds: number[], equipmentType: string): Promise<{ content: string }> => {
    const response = await api.post('/generator/content', { equipmentModelIds, equipmentType });
    return response.data;
  },
  generatePdf: async (content: string, title: string): Promise<Blob> => {
    const response = await api.post('/generator/pdf', { content, title }, { responseType: 'blob' });
    return response.data;
  },
  generateCompliance: async (equipmentModelIds: number[], equipmentType: string): Promise<{ content: string }> => {
    const response = await api.post('/generator/compliance', { equipmentModelIds, equipmentType });
    return response.data;
  },
  generateTechnicalSpecs: async (equipmentModelIds: number[], equipmentType: string): Promise<{ content: string }> => {
    const response = await api.post('/generator/technical-specs', { equipmentModelIds, equipmentType });
    return response.data;
  },
};

// ─── Training Data API ───────────────────────────────────

export const trainingDataAPI = {
  getAll: async (dataType?: string): Promise<TrainingData[]> => {
    const params = dataType ? { dataType } : {};
    const response = await api.get('/training-data', { params });
    return response.data;
  },
  create: async (data: { question: string; answer: string; context: string; dataType: string }): Promise<TrainingData> => {
    const response = await api.post('/training-data', data);
    return response.data;
  },
  generate: async (): Promise<TrainingData[]> => {
    const response = await api.post('/training-data/generate');
    return response.data;
  },
  export: async (dataType?: string): Promise<{ data: string }> => {
    const params = dataType ? { dataType } : {};
    const response = await api.get('/training-data/export', { params });
    return response.data;
  },
  import: async (jsonData: string): Promise<{ message: string }> => {
    const response = await api.post('/training-data/import', { jsonData });
    return response.data;
  },
};

// ─── Config API ──────────────────────────────────────────

export const configAPI = {
  getStatus: async (): Promise<ConfigStatus> => {
    const response = await api.get('/config/status');
    return response.data;
  },
  testLlm: async (): Promise<{ connected: boolean; baseUrl: string; message: string }> => {
    const response = await api.get('/config/llm/test');
    return response.data;
  },
};

export default api;
