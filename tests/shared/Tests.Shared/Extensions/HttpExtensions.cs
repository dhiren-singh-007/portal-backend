/********************************************************************************
 * Copyright (c) 2021,2022 BMW Group AG
 * Copyright (c) 2021,2022 Contributors to the CatenaX (ng) GitHub Organisation.
 *
 * See the NOTICE file(s) distributed with this work for additional
 * information regarding copyright ownership.
 *
 * This program and the accompanying materials are made available under the
 * terms of the Apache License, Version 2.0 which is available at
 * https://www.apache.org/licenses/LICENSE-2.0.
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations
 * under the License.
 *
 * SPDX-License-Identifier: Apache-2.0
 ********************************************************************************/

using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace Org.CatenaX.Ng.Portal.Backend.Tests.Shared.Extensions;

public static class HttpExtensions
{
    public static async Task<T> GetResultFromContent<T>(this HttpResponseMessage response)
    {
        var responseString = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<T>(responseString) ?? throw new InvalidOperationException();
    }
    
    public static HttpContent ToFormContent(this string stringContent, string contentType)
    {
        HttpContent content = new StringContent(stringContent);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return content;
    }
}