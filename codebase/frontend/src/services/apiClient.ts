import axios, {
  type AxiosError,
  type AxiosRequestConfig,
  type InternalAxiosRequestConfig,
} from 'axios'

/**
 * REST client for the LABMEDIS API (contracts/*.md). Attaches the JWT access token to every
 * request and transparently refreshes it on a 401 response (FR-012), so callers never have
 * to think about token lifecycle themselves.
 */

const ACCESS_TOKEN_KEY = 'labmedis.accessToken'
const REFRESH_TOKEN_KEY = 'labmedis.refreshToken'

export function getAccessToken(): string | null {
  return localStorage.getItem(ACCESS_TOKEN_KEY)
}

export function getRefreshToken(): string | null {
  return localStorage.getItem(REFRESH_TOKEN_KEY)
}

export function setTokens(accessToken: string, refreshToken: string): void {
  localStorage.setItem(ACCESS_TOKEN_KEY, accessToken)
  localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken)
}

export function clearTokens(): void {
  localStorage.removeItem(ACCESS_TOKEN_KEY)
  localStorage.removeItem(REFRESH_TOKEN_KEY)
}

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:5443',
})

apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = getAccessToken()
  if (token) {
    config.headers.set('Authorization', `Bearer ${token}`)
  }
  return config
})

let refreshPromise: Promise<string | null> | null = null

async function refreshAccessToken(): Promise<string | null> {
  const refreshToken = getRefreshToken()
  if (!refreshToken) {
    return null
  }

  try {
    const response = await axios.post<{ accessToken: string; refreshToken: string }>(
      `${apiClient.defaults.baseURL}/api/auth/refresh`,
      { refreshToken }
    )
    setTokens(response.data.accessToken, response.data.refreshToken)
    return response.data.accessToken
  } catch {
    clearTokens()
    return null
  }
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as (AxiosRequestConfig & { _retry?: boolean }) | undefined

    if (error.response?.status === 401 && originalRequest && !originalRequest._retry) {
      originalRequest._retry = true

      refreshPromise ??= refreshAccessToken().finally(() => {
        refreshPromise = null
      })

      const newAccessToken = await refreshPromise
      if (newAccessToken) {
        originalRequest.headers = {
          ...originalRequest.headers,
          Authorization: `Bearer ${newAccessToken}`,
        }
        return apiClient(originalRequest)
      }
    }

    return Promise.reject(error)
  }
)
