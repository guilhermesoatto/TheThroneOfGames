{{- define "thethroneofgames.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "thethroneofgames.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{- define "thethroneofgames.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "thethroneofgames.labels" -}}
helm.sh/chart: {{ include "thethroneofgames.chart" . }}
{{ include "thethroneofgames.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- with .Values.commonLabels }}
{{ toYaml . }}
{{- end }}
{{- end }}

{{- define "thethroneofgames.selectorLabels" -}}
app.kubernetes.io/name: {{ include "thethroneofgames.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{- define "thethroneofgames.serviceAccountName" -}}
{{- if .Values.rbac.serviceAccounts.api.create }}
{{- default (include "thethroneofgames.fullname" .) .Values.rbac.serviceAccounts.api.name }}
{{- else }}
{{- default "default" .Values.rbac.serviceAccounts.api.name }}
{{- end }}
{{- end }}
