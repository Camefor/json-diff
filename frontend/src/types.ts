export type DiffKind = 'Added' | 'Removed' | 'Changed' | 'TypeChanged'

export interface FieldMapping {
  from: string
  to: string
}

export interface CompareOptions {
  compareKeys: boolean
  compareValues: boolean
  compareTypes: boolean
  nullStrategy: 'strict' | 'ignore' | 'missing-as-null'
  numericTolerance: number
  floatEpsilon: number
  ignoreArrayOrder: boolean
  arrayKey: string
  caseSensitive: boolean
  ignorePaths: string[]
  whitelistPaths: string[]
  mappings: FieldMapping[]
}

export interface DifferenceSummary {
  total: number
  added: number
  removed: number
  changed: number
  typeChanged: number
  ignored: number
}

export interface JsonDifference {
  path: string
  kind: DiffKind
  oldValue: string | null
  newValue: string | null
  oldType: string
  newType: string
  message: string
}

export interface CompareJsonResponse {
  id: string
  isEqual: boolean
  durationMs: number
  summary: DifferenceSummary
  differences: JsonDifference[]
  createdAt: string
}

export interface CompareJsonRequest {
  oldJson: string
  newJson: string
  name?: string
  options: CompareOptions
}

export interface InterfaceRequest {
  url: string
  method: string
  headers: Record<string, string>
  query: Record<string, string>
  body?: string
}

export interface InterfaceCompareRequest {
  name?: string
  oldRequest: InterfaceRequest
  newRequest: InterfaceRequest
  options: CompareOptions
}

export interface InterfaceResponseMeta {
  statusCode: number
  durationMs: number
  contentType: string
  url: string
}

export interface InterfaceCompareResponse {
  id: string
  result: CompareJsonResponse
  oldResponse: InterfaceResponseMeta
  newResponse: InterfaceResponseMeta
}

export interface BatchCompareItemRequest {
  id?: string
  name: string
  oldJson: string
  newJson: string
  options?: CompareOptions
}

export interface BatchCompareRequest {
  items: BatchCompareItemRequest[]
  options: CompareOptions
}

export interface BatchCompareItemResponse {
  id: string
  name: string
  isEqual: boolean
  error?: string
  result?: CompareJsonResponse
}

export interface BatchCompareResponse {
  id: string
  createdAt: string
  total: number
  equal: number
  different: number
  items: BatchCompareItemResponse[]
}

export interface HistorySummary {
  id: string
  name: string
  sourceType: 'json' | 'interface' | 'batch'
  createdAt: string
  isEqual: boolean
  durationMs: number
  summary: DifferenceSummary
  // 接口类型历史才有值，用于列表展示
  oldUrl?: string
  newUrl?: string
}

export interface HistoryQueryResponse {
  total: number
  page: number
  pageSize: number
  items: HistorySummary[]
}

export interface HistoryRecord extends HistorySummary {
  oldJson: string
  newJson: string
  options: CompareOptions
  result: CompareJsonResponse
  // 接口类型历史才有值，用于跳转回填
  oldRequest?: InterfaceRequest | null
  newRequest?: InterfaceRequest | null
}

export interface CompareProfile {
  name: string
  description: string
  updatedAt: string
  options: CompareOptions
}

export const defaultOptions = (): CompareOptions => ({
  compareKeys: true,
  compareValues: true,
  compareTypes: true,
  nullStrategy: 'strict',
  numericTolerance: 0,
  floatEpsilon: 0.000001,
  ignoreArrayOrder: false,
  arrayKey: '',
  caseSensitive: true,
  ignorePaths: [],
  whitelistPaths: [],
  mappings: [],
})

export const cloneOptions = (options: CompareOptions): CompareOptions => ({
  ...options,
  ignorePaths: [...options.ignorePaths],
  whitelistPaths: [...options.whitelistPaths],
  mappings: options.mappings.map((mapping) => ({ ...mapping })),
})
