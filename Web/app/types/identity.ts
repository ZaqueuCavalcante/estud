export type SsoProviderType = 'AzureAd' | 'GoogleWorkspace' | 'Okta' | 'Auth0' | 'CustomOidc'

export type SsoDomainStatus = 'Pending' | 'Verified'

export interface GetSsoConfigurationDomainOut {
  domain: string
  status: SsoDomainStatus
  verifiedAt: string | null
  txtRecordName: string
  txtRecordValue: string
}

export interface GetSsoConfigurationOut {
  id: string
  providerType: SsoProviderType
  authority: string
  clientId: string
  isActive: boolean
  requireSso: boolean
  createdAt: string
  domains: GetSsoConfigurationDomainOut[]
}

export interface VerifySsoDomainOut {
  domain: string
  status: SsoDomainStatus
  verifiedAt: string | null
}

export interface CheckSsoAvailabilityResponse {
  ssoEnabled: boolean
  ssoRequired: boolean
  providerType: SsoProviderType | null
}

export type RoleBaseType = 'Manager' | 'Teacher' | 'Student' | 'Parent'

export interface TwoFactorEnforcementItem {
  roleId: number
  name: string
  baseType: RoleBaseType
  twoFactorRequired: boolean
}

export interface GetTwoFactorEnforcementOut {
  total: number
  items: TwoFactorEnforcementItem[]
}

export interface SetTwoFactorEnforcementIn {
  roleId: number
  required: boolean
}

export interface SetTwoFactorEnforcementOut {
  roleId: number
  twoFactorRequired: boolean
}
