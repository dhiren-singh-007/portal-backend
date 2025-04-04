/********************************************************************************
 * Copyright (c) 2022 BMW Group AG
 * Copyright (c) 2022 Contributors to the Eclipse Foundation
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

using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.DBAccess.Models;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.PortalEntities.Entities;
using Org.Eclipse.TractusX.Portal.Backend.PortalBackend.PortalEntities.Enums;

namespace Org.Eclipse.TractusX.Portal.Backend.PortalBackend.DBAccess.Repositories;

/// <summary>
/// Repository for writing documents on persistence layer.
/// </summary>
public interface IDocumentRepository
{
    /// <summary>
    /// Creates a document in the persistence layer.
    /// </summary>
    /// <param name="documentName">The documents name</param>
    /// <param name="documentContent">The document itself</param>
    /// <param name="hash">Hash of the document</param>
    /// <param name="mediaTypeId">The documents mediaType</param>
    /// <param name="documentTypeId">the document type id</param>
    /// <param name="documentSize">the document size</param>
    /// <param name="setupOptionalFields">Action to setup the additional fields</param>
    /// <returns>Returns the created document</returns>
    Document CreateDocument(string documentName, byte[] documentContent, byte[] hash, MediaTypeId mediaTypeId, DocumentTypeId documentTypeId, long documentLength, Action<Document>? setupOptionalFields);

    /// <summary>
    /// Gets the document with the given id from the persistence layer.
    /// </summary>
    /// <param name="documentId">Id of the document</param>
    /// <returns>Returns the document</returns>
    Task<Document?> GetDocumentByIdAsync(Guid documentId, IEnumerable<DocumentTypeId> documentTypeIds);

    Task<(Guid DocumentId, DocumentStatusId DocumentStatusId, IEnumerable<Guid> ConsentIds, bool IsSameUser)> GetDocumentDetailsForIdUntrackedAsync(Guid documentId, Guid companyUserId);

    /// <summary>
    /// Gets all documents for the given applicationId, documentId and userId
    /// </summary>
    /// <param name="applicationId">Id of the application</param>
    /// <param name="documentTypeId">Id of the document type</param>
    /// <param name="companyUserId">Id of the user</param>
    /// <returns>A collection of documents</returns>
    Task<(bool IsApplicationAssignedUser, IEnumerable<UploadDocuments> Documents)> GetUploadedDocumentsAsync(Guid applicationId, DocumentTypeId documentTypeId, Guid companyUserId);

    /// <summary>
    /// Gets the documents userid by the document id
    /// </summary>
    /// <param name="documentId">id of the document the user id should be selected for</param>
    /// <param name="companyUserId"></param>
    /// <returns>Returns the user id if a document is found for the given id, otherwise null</returns>
    Task<(Guid DocumentId, bool IsSameUser, bool IsRoleOperator, bool IsStatusConfirmed)> GetDocumentIdWithCompanyUserCheckAsync(Guid documentId, Guid companyUserId);

    /// <summary>
    /// Get the document data and checks if the user 
    /// </summary>
    /// <param name="documentId">id of the document</param>
    /// <param name="userCompanyId"></param>
    /// <returns>Returns the document data</returns>
    Task<(byte[]? Content, string FileName, MediaTypeId MediaTypeId, bool IsUserInCompany)> GetDocumentDataAndIsCompanyUserAsync(Guid documentId, Guid userCompanyId);

    /// <summary>
    /// Gets the document data for the given id and type
    /// </summary>
    /// <param name="documentId">id of the document</param>
    /// <param name="documentTypeId">type of the document</param>
    /// <returns>Returns the document data</returns>
    Task<(byte[] Content, string FileName, MediaTypeId MediaTypeId)> GetDocumentDataByIdAndTypeAsync(Guid documentId, DocumentTypeId documentTypeId);

    /// <summary>
    ///Deleting document record and document file from the portal db/document storage location
    /// </summary>
    /// <param name="documentId">The documentId that should be removed</param>
    void RemoveDocument(Guid documentId);

    /// <summary>
    /// Gets the documents and User by the document id
    /// </summary>
    /// <param name="documentId"></param>
    /// <param name="userCompanyId"></param>
    /// <param name="applicationStatusIds"></param>
    /// <param name="applicationId"></param>
    Task<(Guid DocumentId, DocumentStatusId DocumentStatusId, bool IsSameApplicationUser, DocumentTypeId documentTypeId, bool IsQueriedApplicationStatus, IEnumerable<Guid> applicationId)> GetDocumentDetailsForApplicationUntrackedAsync(Guid documentId, Guid userCompanyId, IEnumerable<CompanyApplicationStatusId> applicationStatusIds);

    /// <summary>
    /// Attaches the document and sets the optional parameters
    /// </summary>
    /// <param name="documentId">Id of the document</param>
    /// <param name="initialize">Action to initialize the entity with values before the change</param>
    /// <param name="modify">Action to set the values that are subject to change</param>
    void AttachAndModifyDocument(Guid documentId, Action<Document>? initialize, Action<Document> modify);

    /// <summary>
    /// Attaches a range of documents optionally initializing them before and modifying them afer the attach
    /// </summary>
    /// <param name="documentData">IEnumerable of tuple of Id, (optional) Action to initialize the entity with values before the change and Action to set the values that are subject to change</param>
    void AttachAndModifyDocuments(IEnumerable<(Guid DocumentId, Action<Document>? Initialize, Action<Document> Modify)> documentData);

    /// <summary>
    /// Gets the document seed data for the given id
    /// </summary>
    /// <param name="documentId">Id of the document</param>
    /// <returns>The document seed data</returns>
    Task<DocumentSeedData?> GetDocumentSeedDataByIdAsync(Guid documentId);

    /// <summary>
    /// Retrieve Document TypeId , Content and validate app link to document
    /// </summary>
    /// <param name="offerId"></param>
    /// <param name="documentId"></param>
    /// <param name="documentTypeIds"></param>
    /// <param name="offerTypeId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<OfferDocumentContentData?> GetOfferDocumentContentAsync(Guid offerId, Guid documentId, IEnumerable<DocumentTypeId> documentTypeIds, OfferTypeId offerTypeId, CancellationToken cancellationToken);

    /// <summary>
    /// Fetch the App Document belongs to same company
    /// </summary>
    /// <param name="documentId"></param>
    /// <param name="iamUserId"></param>
    /// <param name="documentTypeIds"></param>
    /// <param name="offerTypeId"></param>
    /// <returns></returns>
    Task<(IEnumerable<(OfferStatusId OfferStatusId, Guid OfferId, bool IsOfferType)> OfferData, bool IsDocumentTypeMatch, DocumentStatusId DocumentStatusId, bool IsProviderCompanyUser)> GetOfferDocumentsAsync(Guid documentId, Guid userCompanyId, IEnumerable<DocumentTypeId> documentTypeIds, OfferTypeId offerTypeId);

    /// <summary>
    /// Delete List Of Document
    /// </summary>
    /// <param name="documentIds"></param>
    void RemoveDocuments(IEnumerable<Guid> documentIds);

    /// <summary>
    /// Gets the registration document with the given id
    /// </summary>
    /// <param name="documentId">Id of the document</param>
    /// <param name="documentTypeIds">the document types</param>
    /// <returns></returns>
    Task<(byte[] Content, string FileName, bool IsDocumentTypeMatch, MediaTypeId MediaTypeId)> GetDocumentAsync(Guid documentId, IEnumerable<DocumentTypeId> documentTypeIds);

    IAsyncEnumerable<(Guid DocumentId, IEnumerable<Guid> AgreementIds, IEnumerable<Guid> OfferIds)> GetDocumentDataForCleanup(DateTimeOffset dateCreated);
}
